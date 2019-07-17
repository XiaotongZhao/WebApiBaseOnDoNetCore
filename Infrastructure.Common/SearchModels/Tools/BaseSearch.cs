﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Common.SearchModels.Tools
{
    public class BaseSearch
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int Skip { get; set; }
        public int Size { get; set; }
    }
}
