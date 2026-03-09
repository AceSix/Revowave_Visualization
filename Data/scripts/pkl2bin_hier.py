# -*- coding:utf-8 -*-
###################################################################
###   @FilePath: \NASA-GEDI\Data\scripts\pkl2bin_hier.py
###   @Author: AceSix
###   @Date: 2026-03-09 15:28:24
###   @LastEditors: AceSix
###   @LastEditTime: 2026-03-09 18:17:23
###   @Copyright (C) 2026 Brown U. All rights reserved.
###################################################################
import pickle
import pandas as pd
import numpy as np
import sys
import warnings
import random
import yaml
import os
from .utils import _get_rh2wave
h_sample, rh2wave = _get_rh2wave()


warnings.filterwarnings("ignore", category=DeprecationWarning)

CONFIG_FILE_PATH = './yamls/Ardeche.yaml'


# ====== START YAML STUFF ======
with open(CONFIG_FILE_PATH, 'r') as f:
    config = yaml.safe_load(f)

# prints PKL data and stops program
DEBUG_MODE = config.get('debug_mode', False)

# input config
input_config = config.get('input', {})
PKL_FILE = input_config.get('pkl_file')

# processing config
proc_config = config.get('processing', {})
ADAPTIVE_THRESHOLD = proc_config.get('adaptive_threshold', 0)
GEO_BOUNDS = proc_config.get('geo_bounds')
assert len(GEO_BOUNDS)==4, f"geo bounds error" 
CLIP_METERS_ABOVE_RH98 = proc_config.get('clip_meters_above_rh98', 5)
APPLY_SQUARE_ROOT = proc_config.get('apply_square_root', False)

# output config
output_config = config.get('output', {})
OUTPUT_PATH = output_config.get('path')
BASE_FILENAME = output_config.get('base_filename')
if not OUTPUT_PATH or not BASE_FILENAME:
    print("Output error")
    sys.exit(1)

# output filename
OUTPUT_FILENAME = f'{BASE_FILENAME}.bin'
FULL_OUTPUT_PATH = os.path.join(OUTPUT_PATH, OUTPUT_FILENAME)


# ====== END YAML STUFF ======

def adaptive_downsample(waveform, similarity_threshold=ADAPTIVE_THRESHOLD, min_segment_length=3):
    # returns tuple of (downsampled_values, segment_lengths, physical_positions)
    downsampled = []
    segment_lengths = []
    physical_positions = []
    
    wave_len = len(waveform)
    
    i = 0
    accumulated_samples = 0
    while i < wave_len:
        current_value = waveform[i]
        segment_length = 1
        
        j = i + 1
        while j < wave_len:
            if abs(waveform[j] - current_value) <= similarity_threshold:
                segment_length += 1
                j += 1
            else:
                break
        
        # calc physical position as percentage of total
        physical_position = accumulated_samples / wave_len
        
        if segment_length >= min_segment_length:
            segment_mean = np.mean(waveform[i:i+segment_length])
            # segment_mean = waveform[i]
            downsampled.append(segment_mean)
            segment_lengths.append(segment_length)
            physical_positions.append(physical_position)
            accumulated_samples += segment_length
            i += segment_length
        else:
            downsampled.append(current_value)
            segment_lengths.append(1)
            physical_positions.append(physical_position)
            accumulated_samples += 1
            i += 1
    
    return np.array(downsampled), np.array(segment_lengths), np.array(physical_positions)


with open(PKL_FILE, 'rb') as f:
    data = pickle.load(f)

def area_filter(lng, lat):
    return (lng >= GEO_BOUNDS[0]) & (lng <= GEO_BOUNDS[1]) & (lat >= GEO_BOUNDS[2]) & (lat <= GEO_BOUNDS[3])


data_list = []

# Print data keys in debug mode
if DEBUG_MODE:
    print(f'keys: {data.keys()}\n')
    print(f"prop: {data['prop'][0]}\n")
    print(f"prop_rh: {data['prop_rh'][0]}\n")
    print(f"rh: {data['rh'][0]}\n")
    sys.exit()

# Print stats
if ADAPTIVE_THRESHOLD == 0:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD} (off)')
else:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD}')
print(f'Meters clipped above RH98: \t{CLIP_METERS_ABOVE_RH98}m')

count = 0



for i in range(len(data['prop'])):
    # print waveform count
    count += 1
    print(f'\rWaveforms processed: \t\t{count}', end='')

    entry = data['prop'][i]
    prop_rh = data['prop_rh'][i]
    
    latitude = entry['geolocation/latitude_bin0']
    longitude = entry['geolocation/longitude_bin0']

    # latitude = prop_rh['lat_lowestmode']
    # longitude = prop_rh['lon_lowestmode']


    # elevation = entry['geolocation/elevation_bin0']
    # elevation = entry['elev_lowestmode']

    elevation = prop_rh['geolocation/digital_elevation_model']
    elevation_bin0 = entry['geolocation/elevation_bin0']

    if not area_filter(longitude, latitude):
        continue

    # Get instrument and lowestmode coordinates
    instrument_lat = prop_rh['geolocation/latitude_instrument']
    instrument_lon = prop_rh['geolocation/longitude_instrument']
    instrument_alt = prop_rh['geolocation/altitude_instrument']
    
    lowest_lat = prop_rh['lat_lowestmode']
    lowest_lon = prop_rh['lon_lowestmode']
    lowest_elev = prop_rh['elev_lowestmode']

    # Get WGS84 elevation from digital elevation model
    wgs84_elevation = prop_rh['geolocation/digital_elevation_model']

    rh_2 = data['rh'][i][2]
    rh_50 = data['rh'][i][50]
    rh_98 = data['rh'][i][98]
    rh_waveform = data['rh'][i]

    # Adaptive downsampling of raw waveform
    raw_waveform = rh2wave(rh_waveform)
    downsampled_values, segment_lengths, physical_positions = adaptive_downsample(raw_waveform)

    # apply square root then normalize
    if downsampled_values.size > 0:
        # non negative
        values_to_process = np.maximum(0, downsampled_values)

        # apply square root
        if APPLY_SQUARE_ROOT:
            values_to_process = np.sqrt(values_to_process + 1e-6)

        # NORMALIZE sqrt values
        current_sum = np.sum(values_to_process)
        normalized_values = values_to_process / current_sum
        processed_values = normalized_values

    # processed_values = downsampled_values
    # clip spindles
    clip_height_above_ground = rh_98 + CLIP_METERS_ABOVE_RH98
    clip_elevation_threshold = elevation + clip_height_above_ground

    waveform_vertical_range = max(0.01, elevation_bin0 - elevation)

    clipped_values = []
    clipped_lengths = []
    clipped_positions = []

    if processed_values.size > 0:
        for j in range(len(processed_values)):
            sample_elevation = elevation_bin0 - (physical_positions[j] * waveform_vertical_range)

            if sample_elevation <= clip_elevation_threshold:
                clipped_values.append(processed_values[j])
                clipped_lengths.append(segment_lengths[j])
                clipped_positions.append(physical_positions[j])

    if not clipped_values:
        values_str = ""
        lengths_str = ""
        positions_str = ""
    else:
        values_str = ','.join(map(str, clipped_values))
        lengths_str = ','.join(map(str, clipped_lengths))
        positions_str = ','.join(map(str, clipped_positions))
    
    # # Convert to strings and combine with delimiter
    # values_str = ','.join(map(str, downsampled_values))
    # lengths_str = ','.join(map(str, segment_lengths))
    # positions_str = ','.join(map(str, physical_positions))

    data_list.append({
        'latitude': latitude,
        'longitude': longitude,
        'elevation': elevation,
        'instrument_lat': instrument_lat,
        'instrument_lon': instrument_lon, 
        'instrument_alt': instrument_alt,
        'lowest_lat': lowest_lat,
        'lowest_lon': lowest_lon,
        'lowest_elev': lowest_elev,
        'wgs84_elevation': wgs84_elevation,
        'rh2': rh_2,
        'rh50': rh_50,
        'rh98': rh_98,
        'rh_waveform': rh_waveform_str,
        'raw_waveform_values': values_str,
        'raw_waveform_lengths': lengths_str,
        'raw_waveform_positions': positions_str,
    })

df = pd.DataFrame(data_list)
df.to_csv(f'{OUTPUT_PATH}{OUTPUT_FILENAME}', index=False)
print(f'\nOutput path: \t\t\t{OUTPUT_PATH}')
print(f'Output filename: \t\t{OUTPUT_FILENAME}')