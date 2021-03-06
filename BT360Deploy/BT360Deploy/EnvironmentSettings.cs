﻿// (c) Copyright 2016 Axon Olympus 
// This source is subject to the Microsoft Public License
// See https://opensource.org/licenses/ms-pl. 
// All other rights reserved.
using System;
namespace AxonOlympus.BT360Deploy
{
    /// <summary>
    /// Class which contains some fields which must be provided with each call to the BizTalk360 API
    /// </summary>
    public class EnvironmentSettings
    {
        public Guid id { get; set; }
        public int licenseEdition { get; set; }
    }
}
