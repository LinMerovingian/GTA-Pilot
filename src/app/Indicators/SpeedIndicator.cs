﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GTAPilot;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public class SpeedIndicator : Indicator
{
    protected override InputType Type { get { return InputType.Speed; } }

    public SpeedIndicator()
    {

        // 140: night
        TuningValues.Add(new Hsv(0, 0, 140));

        // 75: daylight
        TuningValues.Add(new Hsv(0, 0, 75));

    }

    protected override Rectangle FastROI
    {
        get
        {
            if (Hints.AttitudeIndicator.Center.X > 0)
            {
                var r = (int)Hints.PitchIndicator.Radius;
                var d = (int)Hints.PitchIndicator.Radius * 2;
                return (new Rectangle((int)Hints.PitchIndicator.Center.X + r + Hints.PitchRect.Left, (int)Hints.PitchIndicator.Center.Y - r + Hints.PitchRect.Top - 110, 150, 150));
            }
            else
            {
                return default(Rectangle);
            }
        }
    }

    protected override bool ProcessFrameCore(IndicatorData data, ref double ObservedValue)
    {
        DateTime this_time = DateTime.Now;
        DateTime this_last_time = last_time;
        double this_last_value = last_value;


        var vs_hsv = data.Frame.Convert<Hsv, byte>();
        {
            var markedup_circles = vs_hsv[2].Convert<Bgr, byte>();
            {
                var circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 20, 10, 180, 30, 100);
                if (circles.Length == 0)
                {
                    circles = CvInvoke.HoughCircles(vs_hsv[2], HoughType.Gradient, 2.0, 20, 10, 100, 30, 100);
                    foreach (var c in circles)
                    {
                        CvInvoke.Circle(markedup_circles, Point.Round(c.Center), (int)c.Radius, new Bgr(Color.Red).MCvScalar, 1);
                    }
                }
                if (circles.Length > 0)
                {
                    var airspeed_circle = circles[0];
                    var best_circle = airspeed_circle;

                  //  var vs_hsv2 = ProcessedFrame.Convert<Hsv, byte>())
                    {
                       // var vs_blackimg = vs_hsv.InRange(HsvTuner.Low, HsvTuner.High);
                        var vs_blackimg = vs_hsv.InRange(TuningValue, new Hsv(180, 255, 255));
                        {
                            circles = CvInvoke.HoughCircles(vs_blackimg, HoughType.Gradient, 2.0, 40, 10, 20, 40, 50);
                            if (circles.Length > 0)
                            {

                                var margin = 4;
                                best_circle = circles[0];
                                var d = (int)best_circle.Radius * 2;
                                var r = (int)best_circle.Radius;


                                var new_rect = new Rectangle((int)best_circle.Center.X - r - margin, (int)best_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2);

                                var speedFrame = data.Frame.Copy(new_rect);
                                

                                var vs_blackimg2 = vs_blackimg.Copy(new Rectangle((int)best_circle.Center.X - r - margin, (int)best_circle.Center.Y - r - margin, d + margin * 2, d + margin * 2));
                                {
                                    Mat vspeedMask = new Mat(vs_blackimg2.Size, DepthType.Cv8U, 3);
                                    {
                                        vspeedMask.SetTo(new MCvScalar(1));
                                        CvInvoke.Circle(vspeedMask, Point.Round(new PointF(r + margin, r + margin)), (int)(r - (r * 0)), new Bgr(Color.White).MCvScalar, -1);

                                        var vspeed_inner_only = vs_blackimg2.Copy(vspeedMask.ToImage<Gray, byte>());
                                        {

                                            var cannyEdges3 = new Mat();
                                            {
                                                CvInvoke.Canny(vspeed_inner_only, cannyEdges3, 10, 100);
                                                var lines = CvInvoke.HoughLinesP(cannyEdges3, 1, Math.PI / 45.0, 4, 14, 4).OrderByDescending(p => p.Length).ToList();

                                                var center_size = 25;
                                                var center_point = new Point((speedFrame.Width / 2), (speedFrame.Height / 2));
                                                var center_box_point = new Point((speedFrame.Width / 2) - (center_size / 2), (speedFrame.Height / 2) - (center_size / 2) + 4);
                                                Rectangle center = new Rectangle(center_box_point, new Size(center_size, center_size));

                                                var bestLines = new List<Tuple<double, LineSegment2D>>();
                                                var markedup_frame = vspeed_inner_only.Convert<Bgr, byte>();
                                                {

                                                    foreach (var line in lines)
                                                    {
                                                        CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Yellow).MCvScalar, 1);

                                                        if (center.Contains(line.P1) || center.Contains(line.P2))
                                                        {
                                                            Point other_point;

                                                            if (center.Contains(line.P1))
                                                            {
                                                                other_point = line.P2;
                                                            }
                                                            else
                                                            {
                                                                other_point = line.P1;
                                                            }
                                                            bestLines.Add(new Tuple<double, LineSegment2D>(Math2.GetDistance(other_point, center_point), line));
                                                        }
                                                    }
                                                    bestLines = bestLines.OrderByDescending(l => l.Item1).ToList();

                                                    CvInvoke.Rectangle(markedup_frame, center, new Bgr(Color.Yellow).MCvScalar, 1);

                                                    bool didFinish = false;
                                                    foreach (var lineX in bestLines)
                                                    {
                                                        var line = lineX.Item2;
                                                        CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);

                                                        if (center.Contains(line.P1) || center.Contains(line.P2))
                                                        {
                                                            Point other_point;
                                                            if (center.Contains(line.P1))
                                                            {
                                                                other_point = line.P2;
                                                            }
                                                            else
                                                            {
                                                                other_point = line.P1;
                                                            }

                                                            LineSegment2D final_line = new LineSegment2D(center_point, other_point);
                                                            LineSegment2D baseLine = new LineSegment2D(new Point((speedFrame.Width / 2), 0), new Point((speedFrame.Width / 2), (speedFrame.Height / 2)));

                                                            var v_angle = Math2.angleBetween2Lines(line, baseLine);
                                                            v_angle = v_angle * (180 / Math.PI);
                                                            var v_angle_o = v_angle;
                                                            if (v_angle >= 180 && v_angle <= 270 && v_angle > 0)
                                                            {
                                                                var is_bottom = other_point.Y >= center_point.Y && other_point.X <= center_point.X;
                                                                if (is_bottom)
                                                                {
                                                                    // angle OK
                                                                }
                                                                else
                                                                {
                                                                    v_angle -= 180;
                                                                }
                                                            }
                                                            else if (v_angle <= 0 && v_angle >= -90)
                                                            {
                                                                var is_bottom = other_point.Y > center_point.Y || other_point.X > center_point.X;
                                                                if (is_bottom)
                                                                {
                                                                    v_angle += 180;
                                                                }
                                                                else
                                                                {
                                                                    v_angle += 360;
                                                                }
                                                            }

                                                          //  var center_full = new Point(center_point.X + base_rect.X, center_point.Y + base_rect.Y);
                                                          //  var other_full = new Point(other_point.X + base_rect.X, other_point.Y + base_rect.Y);

                                                          //  CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Black).MCvScalar, 3);
                                                          //  CvInvoke.Line(data.Frame, center_full, other_full, new Bgr(Color.Lime).MCvScalar, 1);

                                                         //   CvInvoke.Line(ProcessedFrame, center_point, other_point, new Bgr(Color.Yellow).MCvScalar, 2);

                                                            // 0-360 degrees
                                                            // 0-180 knots

                                                            var knots = (v_angle / 2);
                                                            if (knots > 175) knots = 0;

                                                            ObservedValue = knots;
                                                            IntermediateFrameBgr = markedup_frame;

                                                            var change = Math.Abs(ObservedValue - last_value);
                                                            if (change > 40 && num_rejected_values < 10)
                                                            {
                                                                num_rejected_values++;
                                                                return false;
                                                            }

                                                            last_value = ObservedValue;
                                                            num_rejected_values = 0;

                                                            didFinish = true;
                                                            CvInvoke.Line(markedup_frame, line.P1, line.P2, new Bgr(Color.Red).MCvScalar, 1);
                                                            break;
                                                        }
                                                    }



                                                    if (!didFinish)
                                                    {
                                                        LastAction = "Didn't finish. " + lines.Count;
                                                    }
                                                    else
                                                    {


                                                     //   Trace.WriteLine(string.Format("SPEED: {0} {1}", ObservedValue, LastGoodValue));
                                                    }
                                                    return didFinish;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    // IntermediateFrameBgr = markedup_circles;
                }
                return false;
            }
        }
    }

    double last_value = 0;
    int num_rejected_values = 0;
    DateTime last_time = DateTime.Now;
}