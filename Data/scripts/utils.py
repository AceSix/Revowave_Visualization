# -*- coding:utf-8 -*-
###################################################################
###   @FilePath: \NASA-GEDI\Data\scripts\utils.py
###   @Author: AceSix
###   @Date: 2026-03-09 18:16:36
###   @LastEditors: AceSix
###   @LastEditTime: 2026-03-09 18:16:36
###   @Copyright (C) 2026 Brown U. All rights reserved.
###################################################################
import numpy as np

def _get_rh2wave(start=-10, end=70, step=0.3):
    h_sample = np.arange(start, end, step)[1:]
    def rh2wave(rh):
        signal = np.arange(101)
        h_points = np.interp(np.arange(start, end, step), rh, signal)
        wave = h_points[1:] - h_points[:-1]
        return wave/wave.sum()
    return h_sample, rh2wave 