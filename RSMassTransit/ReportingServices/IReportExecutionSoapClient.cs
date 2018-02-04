﻿// Copyright (C) 2018 (to be determined)

using System;
using RSMassTransit.ReportingServices.Execution;

namespace RSMassTransit.ReportingServices
{
    internal interface IReportExecutionSoapClient
        : ReportExecutionServiceSoap, IDisposable
    { }

    namespace Execution
    {
        partial class ReportExecutionServiceSoapClient
            : IReportExecutionSoapClient
        { }
    }
}
