﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Algorithms.QuickSort.Service
{
    public interface IQuickSortService
    {
        List<List<int>> QuickSort(int[] datas);
    }
}
