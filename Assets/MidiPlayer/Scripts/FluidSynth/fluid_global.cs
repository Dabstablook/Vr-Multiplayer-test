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
#if UNITY_EDITOR
#else
using System.Diagnostics;
#endif

namespace MidiPlayerTK
{
    //    public enum fluid_log_level
    //    {
    //        FLUID_PANIC,   /**< The synth can't function correctly any more */
    //        FLUID_ERR,     /**< Serious error occurred */
    //        FLUID_WARN,    /**< Warning */
    //        FLUID_INFO,    /**< Verbose informational messages */
    //        FLUID_DBG,     /**< Debugging messages */
    //        LAST_LOG_LEVEL /**< @warning This symbol is not part of the public API and ABI stability guarantee and may change at any time! */
    //    }

    //    static public class fluid_global
    //    {
    //        public const int FLUID_OK = 0;
    //        public const int FLUID_FAILED = -1;

    //        static public void FLUID_LOG(fluid_log_level level, string fmt, params object[] list)
    //        {
    //#if UNITY_EDITOR
    //            if (level == fluid_log_level.FLUID_INFO)
    //                Debug.LogFormat(fmt, list);
    //            else if (level == fluid_log_level.FLUID_WARN)
    //                Debug.LogWarningFormat(fmt, list);
    //            else
    //                Debug.LogErrorFormat(fmt, list);
    //#else
    //            Debug.WriteLine(string.Format(fmt, list));
    //#endif
    //        }

    //    }
}
