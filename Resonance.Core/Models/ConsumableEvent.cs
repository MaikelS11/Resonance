﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Models
{
    public class ConsumableEvent
    {
        public Int64 Id { get; set; }
        public string EventName { get; set; }
        public string FunctionalKey { get; set; }
        public string DeliveryKey { get; set; }
        public DateTime InvisibleUntilUtc { get; set; }
        public string Payload { get; set; }
        internal Int64? PayloadId { get; set; }
    }

    public class ConsumableEvent<T> : ConsumableEvent
    {
        new public T Payload { get; set; }
    }
}
