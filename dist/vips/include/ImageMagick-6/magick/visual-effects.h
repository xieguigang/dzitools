/*
  Copyright 1999 ImageMagick Studio LLC, a non-profit organization
  dedicated to making software imaging solutions freely available.
  
  You may not use this file except in compliance with the License.  You may
  obtain a copy of the License at
  
    https://imagemagick.org/script/license.php
  
  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.

  MagickCore image visual effects methods.
*/
#ifndef MAGICKCORE_VISUAL_EFFECTS_H
#define MAGICKCORE_VISUAL_EFFECTS_H

#include "magick/draw.h"

#if defined(__cplusplus) || defined(c_plusplus)
extern "C" {
#endif

typedef enum
{
  UndefinedNoise,
  UniformNoise,
  GaussianNoise,
  MultiplicativeGaussianNoise,
  ImpulseNoise,
  LaplacianNoise,
  PoissonNoise,
  RandomNoise
} NoiseType;

extern MagickExport Image
  *AddNoiseImage(const Image *,const NoiseType,ExceptionInfo *),
  *AddNoiseImageChannel(const Image *,const ChannelType,const NoiseType,
    ExceptionInfo *),
  *BlueShiftImage(const Image *,const double,ExceptionInfo *),
  *CharcoalImage(const Image *,const double,const double,ExceptionInfo *),
  *ColorizeImage(const Image *,const char *,const PixelPacket,ExceptionInfo *),
  *ColorMatrixImage(const Image *,const KernelInfo *kernel,ExceptionInfo *),
  *ImplodeImage(const Image *,const double,ExceptionInfo *),
  *MorphImages(const Image *,const size_t,ExceptionInfo *),
  *PolaroidImage(const Image *,const DrawInfo *,const double,ExceptionInfo *),
  *SepiaToneImage(const Image *,const double,ExceptionInfo *),
  *ShadowImage(const Image *,const double,const double,const ssize_t,
    const ssize_t,ExceptionInfo *),
  *SketchImage(const Image *,const double,const double,const double,
    ExceptionInfo *),
  *SteganoImage(const Image *,const Image *,ExceptionInfo *),
  *StereoImage(const Image *,const Image *,ExceptionInfo *),
  *StereoAnaglyphImage(const Image *,const Image *,const ssize_t,const ssize_t,
     ExceptionInfo *),
  *SwirlImage(const Image *,double,ExceptionInfo *),
  *TintImage(const Image *,const char *,const PixelPacket,ExceptionInfo *),
  *VignetteImage(const Image *,const double,const double,const ssize_t,
    const ssize_t,ExceptionInfo *),
  *WaveImage(const Image *,const double,const double,ExceptionInfo *),
  *WaveletDenoiseImage(const Image *,const double,const double,ExceptionInfo *);

extern MagickExport MagickBooleanType
  PlasmaImage(Image *,const SegmentInfo *,size_t,size_t),
  SolarizeImage(Image *,const double),
  SolarizeImageChannel(Image *,const ChannelType,const double,ExceptionInfo *);

#if defined(__cplusplus) || defined(c_plusplus)
}
#endif

#endif
