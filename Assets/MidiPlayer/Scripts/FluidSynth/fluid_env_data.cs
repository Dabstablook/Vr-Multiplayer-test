/* FluidSynth - A Software Synthesizer
 *
 * This file is part of the Maestro MPTK C# adaptation of FluidSynth.
 *
 * FluidSynth portions Copyright (C) 2003  Peter Hanappe and others.
 * Modified: 2026-08-11 - LGPL notice added; C# adaptation for Maestro MPTK.
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; see _fluidsynth_LICENSE.txt.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MidiPlayerTK
{

    /*
     * envelope data
     */
    public class fluid_env_data
    {
        public uint count;
        public float coeff;
        public float incr;
        public float min;
        public float max;
        public override string ToString()
        {
            return string.Format("count:{0} coeff:{1} incr:{2} min:{3} max:{4}", count, coeff, incr, min, max);
        }
    }

    /* Indices for envelope tables */
    public enum fluid_voice_envelope_index
    {
        FLUID_VOICE_ENVDELAY,
        FLUID_VOICE_ENVATTACK,
        FLUID_VOICE_ENVHOLD,
        FLUID_VOICE_ENVDECAY,
        FLUID_VOICE_ENVSUSTAIN,
        FLUID_VOICE_ENVRELEASE,
        FLUID_VOICE_ENVFINISHED,
        FLUID_VOICE_ENVLAST
    }
}
