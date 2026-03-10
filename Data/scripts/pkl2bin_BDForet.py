# -*- coding:utf-8 -*-
###################################################################
###   @FilePath: \NASA-GEDI\Data\scripts\pkl2bin_BDForet.py
###   @Author: AceSix
###   @Date: 2026-03-09 15:28:24
###   @LastEditors: AceSix
###   @LastEditTime: 2026-03-10 01:11:19
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
from tqdm import tqdm

def _get_rh2wave(start=-10, end=70, step=0.3):
    h_sample = np.arange(start, end, step)[1:]
    def rh2wave(rh):
        signal = np.arange(101)
        h_points = np.interp(np.arange(start, end, step), rh, signal)
        wave = h_points[1:] - h_points[:-1]
        return wave/wave.sum()
    return h_sample, rh2wave 
h_sample, rh2wave = _get_rh2wave()


warnings.filterwarnings("ignore", category=DeprecationWarning)

CONFIG_FILE_PATH = './yamls/Ardeche.yaml'


# ====== START YAML STUFF ======
with open(CONFIG_FILE_PATH, 'r') as f:
    config = yaml.safe_load(f)


# input config
input_config = config.get('input', {})
FP_FILE = input_config.get('footprint_file')
CLUSTER_FILE = input_config.get('cluster_file')

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
OUTPUT_FILENAME = f'{BASE_FILENAME}'
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
        physical_position = h_sample[i]
        
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


with open(FP_FILE, 'rb') as f:
    data = pickle.load(f)
with open(CLUSTER_FILE, 'rb') as f:
    cluster_data = pickle.load(f)

footprints = []
for k, v in data.items():
    footprints += v

labels = cluster_data['labels']
sub_labels = cluster_data['sub_labels']
clusters = cluster_data['clusters']
sub_clusters = cluster_data['sub_clusters']

for i, fp in enumerate(footprints):
    fp['cluster'] = labels[i]
    fp['sub_cluster'] = sub_labels[i]



# Print stats
if ADAPTIVE_THRESHOLD == 0:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD} (off)')
else:
    print(f'Adaptive sampling threshold: \t{ADAPTIVE_THRESHOLD}')
print(f'Meters clipped above RH98: \t{CLIP_METERS_ABOVE_RH98}m')



def area_filter(fp):
    lat = fp['lat_lowestmode']
    lng = fp['lon_lowestmode']
    return (lng >= GEO_BOUNDS[0]) & (lng <= GEO_BOUNDS[1]) & (lat >= GEO_BOUNDS[2]) & (lat <= GEO_BOUNDS[3])




########################################################################################
############################     extract footprint data      ###########################
########################################################################################
footprints = filter(area_filter, footprints)
footprints = list(footprints)
downsampled, positions = [], []
for fp in tqdm(footprints):
    rh = fp['rh']
    waveform = rh2wave(rh)

    # Adaptive downsampling of raw waveform
    downsampled_values, segment_lengths, physical_positions = adaptive_downsample(waveform)
    # apply square root
    if APPLY_SQUARE_ROOT:
        downsampled_values = np.sqrt(downsampled_values + 1e-6)
    # clip spindles
    clip_height_above_ground = rh[98] + CLIP_METERS_ABOVE_RH98
    downsampled_values[physical_positions>clip_height_above_ground] = 0

    downsampled.append(downsampled_values)
    positions.append(physical_positions)


sample_counts = np.array([len(ds) for ds in downsampled]).astype(np.int32)
downsampled = np.concatenate(downsampled, 0).astype(np.float32)
positions = np.concatenate(positions, 0).astype(np.float32)
longitudes = np.array([fp['lon_lowestmode'] for fp in footprints]).astype(np.float32)
latitudes = np.array([fp['lat_lowestmode'] for fp in footprints]).astype(np.float32)
elevations = np.array([fp['geolocation/digital_elevation_model'] for fp in footprints]).astype(np.float32)
instrument_lons = np.array([fp['geolocation/longitude_instrument'] for fp in footprints]).astype(np.float32)
instrument_lats = np.array([fp['geolocation/latitude_instrument'] for fp in footprints]).astype(np.float32)
instrument_alts = np.array([fp['geolocation/altitude_instrument'] for fp in footprints]).astype(np.float32)

N = len(sample_counts)
with open(f'{OUTPUT_PATH}{OUTPUT_FILENAME}.bin', "wb") as f:
    np.array([N], dtype=np.int32).tofile(f)
    sample_counts.tofile(f)
    
    downsampled.tofile(f)
    positions.tofile(f)

    longitudes.tofile(f)
    latitudes.tofile(f)
    elevations.tofile(f)
    instrument_lons.tofile(f)
    instrument_lats.tofile(f)
    instrument_alts.tofile(f)

print(f'\nOutput path: \t\t\t{OUTPUT_PATH}')
print(f'Output filename: \t\t{OUTPUT_FILENAME}')



########################################################################################
############################      extract cluster data       ###########################
########################################################################################
coords = np.c_[longitudes, latitudes]
labels = np.array([fp['cluster'] for fp in footprints])
sub_labels = np.array([fp['sub_cluster'] for fp in footprints])
coord_center = np.array([ 4.41674461, 44.79504429])
coord_std = 0.2411022236782912

c_longitudes, c_latitudes, c_elevations = [], [], []
c_downsampled, c_positions = [], []
for i, c_fp in enumerate(clusters):
    rh = c_fp['rh_center']
    waveform = rh2wave(rh)
    c_coords = c_fp["mu_s"]*coord_std + coord_center
    c_longitudes.append(c_coords[0])
    c_latitudes.append(c_coords[1])
    
    closest = np.linalg.norm(coords - c_coords, axis=1).argmin()
    c_elevations.append(elevations[closest])

    # Adaptive downsampling of raw waveform
    downsampled_values, segment_lengths, physical_positions = adaptive_downsample(waveform)
    # apply square root
    if APPLY_SQUARE_ROOT:
        downsampled_values = np.sqrt(downsampled_values + 1e-6)
    # clip spindles
    clip_height_above_ground = rh[98] + CLIP_METERS_ABOVE_RH98
    downsampled_values[physical_positions>clip_height_above_ground] = 0

    c_downsampled.append(downsampled_values)
    c_positions.append(physical_positions)


sample_counts = np.array([len(ds) for ds in c_downsampled]).astype(np.int32)
c_downsampled = np.concatenate(c_downsampled, 0).astype(np.float32)
c_positions = np.concatenate(c_positions, 0).astype(np.float32)
c_longitudes = np.array(c_longitudes).astype(np.float32)
c_latitudes = np.array(c_latitudes).astype(np.float32)
c_elevations = np.array(c_elevations).astype(np.float32)
c_elevations_sim = c_elevations + 1

N = len(sample_counts)
with open(f'{OUTPUT_PATH}{OUTPUT_FILENAME}_cluster.bin', "wb") as f:
    np.array([N], dtype=np.int32).tofile(f)
    sample_counts.tofile(f)
    
    c_downsampled.tofile(f)
    c_positions.tofile(f)

    c_longitudes.tofile(f)
    c_latitudes.tofile(f)
    c_elevations.tofile(f)
    c_longitudes.tofile(f)
    c_latitudes.tofile(f)
    c_elevations_sim.tofile(f)


########################################################################################
############################      extract sub_cluster data       ###########################
########################################################################################
c_longitudes, c_latitudes, c_elevations = [], [], []
c_downsampled, c_positions = [], []
for i, c_fp in enumerate(sub_clusters):
    rh = c_fp['rh_center']
    waveform = rh2wave(rh)
    c_coords = c_fp["mu_s"]*coord_std + coord_center
    c_longitudes.append(c_coords[0])
    c_latitudes.append(c_coords[1])

    closest = np.linalg.norm(coords - c_coords, axis=1).argmin()
    c_elevations.append(elevations[closest])

    # Adaptive downsampling of raw waveform
    downsampled_values, segment_lengths, physical_positions = adaptive_downsample(waveform)
    # apply square root
    if APPLY_SQUARE_ROOT:
        downsampled_values = np.sqrt(downsampled_values + 1e-6)
    # clip spindles
    clip_height_above_ground = rh[98] + CLIP_METERS_ABOVE_RH98
    downsampled_values[physical_positions>clip_height_above_ground] = 0

    c_downsampled.append(downsampled_values)
    c_positions.append(physical_positions)


sample_counts = np.array([len(ds) for ds in c_downsampled]).astype(np.int32)
c_downsampled = np.concatenate(c_downsampled, 0).astype(np.float32)
c_positions = np.concatenate(c_positions, 0).astype(np.float32)
c_longitudes = np.array(c_longitudes).astype(np.float32)
c_latitudes = np.array(c_latitudes).astype(np.float32)
c_elevations = np.array(c_elevations).astype(np.float32)
c_elevations_sim = c_elevations + 1

N = len(sample_counts)
with open(f'{OUTPUT_PATH}{OUTPUT_FILENAME}_subcluster.bin', "wb") as f:
    np.array([N], dtype=np.int32).tofile(f)
    sample_counts.tofile(f)
    
    c_downsampled.tofile(f)
    c_positions.tofile(f)

    c_longitudes.tofile(f)
    c_latitudes.tofile(f)
    c_elevations.tofile(f)
    c_longitudes.tofile(f)
    c_latitudes.tofile(f)
    c_elevations_sim.tofile(f)

