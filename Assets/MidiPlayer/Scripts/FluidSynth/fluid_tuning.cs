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
namespace MidiPlayerTK
{
    public class fluid_tuning
    {
        public string name;
        public int bank;
        public int prog;
        public float[] pitch; //[128];  /* the pitch of every key, in cents */

        //#define fluid_tuning_get_bank(_t) ((_t)->bank)
        //#define fluid_tuning_get_prog(_t) ((_t)->prog)
        //#define fluid_tuning_get_pitch(_t, _key) ((_t)->pitch[_key])
        //#define fluid_tuning_get_all(_t) (&(_t)->pitch[0])



        public fluid_tuning(string pname, int pbank, int pprog)
        {
            name = pname;
            bank = pbank;
            prog = pprog;
            pitch = new float[128];
            for (int i = 0; i < 128; i++)
                pitch[i] = i * 100.0f;
        }

        void fluid_tuning_set_name(string pname)
        {
            name = pname;
        }

        static string fluid_tuning_get_name(fluid_tuning tuning)
        {
            return tuning.name;
        }

        void fluid_tuning_set_key(int key, float ppitch)
        {
            pitch[key] = ppitch;
        }

        void fluid_tuning_set_octave(float[] pitch_deriv)
        {
            for (int i = 0; i < 128; i++)
            {
                pitch[i] = i * 100.0f + pitch_deriv[i % 12];
            }
        }

        void fluid_tuning_set_all(float[] ppitch)
        {
            int i;

            for (i = 0; i < 128; i++)
            {
                pitch[i] = ppitch[i];
            }
        }

        void fluid_tuning_set_pitch(int key, float ppitch)
        {
            if ((key >= 0) && (key < 128))
            {
                pitch[key] = ppitch;
            }
        }
    }
}
