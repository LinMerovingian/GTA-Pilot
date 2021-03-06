﻿namespace GTAPilot
{
    class PIDConfig
    {
        public PID.Range PV;
        public PID.Range OV;
        public PID.Gain Gain;
    }

    class FlightComputerConfig
    {
        public static double MIN_AXIS_VALUE = -18000;
        public static double MAX_AXIS_VALUE = 18000;

        public static PIDConfig Roll = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 3,
                I = 0,
                D = 0.2
            },
            PV = new PID.Range { Min = -50, Max = 50 },
            OV = new PID.Range { Min = MIN_AXIS_VALUE, Max = MAX_AXIS_VALUE },
        };

        public static PIDConfig Pitch = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 2.8,
                I = 0.2,
                D = 0.2
            },
            PV = new PID.Range { Min = -90, Max = 90 },
            OV = new PID.Range { Min = MIN_AXIS_VALUE, Max = MAX_AXIS_VALUE },
        };
        public static PIDConfig Speed = new PIDConfig
        {
            Gain = new PID.Gain
            {
                P = 2,
                I = 0.1,
                D = 0,
            },
            PV = new PID.Range { Min = 0, Max = 175 },
            OV = new PID.Range { Min = 0, Max = 255+235 },
        };
    }
}
