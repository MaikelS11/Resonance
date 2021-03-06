﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Resonance.Models
{
    public class TopicEvent
    {
        public Int64? Id { get; set; }
        public Int64 TopicId { get; set; }
        public string EventName { get; set; }
        public DateTime? PublicationDateUtc { get; set; }
        public DateTime ExpirationDateUtc { get; set; }
        public string FunctionalKey { get; set; }
        public int Priority { get; set; }
        public Int64? PayloadId { get; set; }
        public Dictionary<string,string> Headers { get; set; }
    }
}
