﻿namespace GTAPilot.Indicators
{
    interface ISimpleIndicator
    {
        double CachedTuningValue { get; }
        double LastGoodValue { get; }

        double ReadValue(IndicatorData data, DebugState debugState);
    }
}
