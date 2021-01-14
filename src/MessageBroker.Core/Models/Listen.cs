﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageBroker.Core.Models
{
    public ref struct Listen
    {
        public Guid Id { get; init; }
        public string QueueName { get; init; }
    };
}
