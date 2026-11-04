/* FluidSynth - A Software Synthesizer
 *
 * Copyright (C) 2003  Peter Hanappe and others.
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
 * License along with this library; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA
 * 02110-1301, USA
 */

/**
  CHANGES
    - Adapted for Unity, Thierry Bachmann, March 2020

 * Applies a low- or high-pass filter with variable cutoff frequency and quality factor
 * for a given biquad transfer function:
 *          b0 + b1*z^-1 + b2*z^-2
 *  H(z) = ------------------------
 *          a0 + a1*z^-1 + a2*z^-2
 *
 * Also modifies filter state accordingly.
 * @param iir_filter Filter parameter
 * @param dsp_buf Pointer to the synthesized audio data
 * @param count Count of samples in dsp_buf
 */
/*
 * Variable description:
 * - dsp_a1, dsp_a2: Filter coefficients for the the previously filtered output signal
 * - dsp_b0, dsp_b1, dsp_b2: Filter coefficients for input signal
 * - coefficients normalized to a0
 *
 * A couple of variables are used internally, their results are discarded:
 * - dsp_i: Index through the output buffer
 * - dsp_centernode: delay line for the IIR filter
 * - dsp_hist1: same
 * - dsp_hist2: same
 */

using UnityEngine;

namespace MidiPlayerTK
{

   
    public class fluid_iir_filter_64: fluid_iir_filter
    {

        fluid_iir_filter_type type; /* specifies the type of this filter */
        fluid_iir_filter_flags flags; /* additional flags to customize this filter */

        /* filter coefficients */
        /* The coefficients are normalized to a0. */
        /* b0 and b2 are identical => b02 */
        double b02;              /* b0 / a0 */
        double b1;              /* b1 / a0 */
        double a1;              /* a0 / a0 */
        double a2;              /* a1 / a0 */

        double hist1, hist2;      /* Sample history for the IIR filter */
        bool filter_startup;             /* Flag: If set, the filter will be set directly.
					   Else it changes smoothly. */

        double fres;              /* the resonance frequency, in cents (not absolute cents) */
        double last_fres;         /* Current smoothed resonance frequency of the IIR filter, in Hz */
        double fres_incr;         /* Linear increment of last_fres per sample */
        int fres_incr_count;      /* Number of samples left for the smoothed last_fres adjustment */
        double q_lin;             /* Target q-factor on a linear scale */
        double last_q;            /* Current smoothed q-factor on a linear scale */
        double q_incr;            /* Linear increment of last_q per sample */
        int q_incr_count;         /* Number of samples left for the smoothed last_q adjustment */

        const double Q_MIN = 0.001d;

        int FLUID_BUFSIZE;
        float output_rate;

        public fluid_iir_filter_64(int bufsize)
        {
            //Debug.Log("Init iir filter 64");
            FLUID_BUFSIZE = bufsize;
        }

        public override void fluid_iir_filter_apply(float[] dsp_buf, int count)
        {
            // MPTK Specific, check effect disabled done above
            if (type == fluid_iir_filter_type.FLUID_IIR_DISABLED || last_q < Q_MIN)
            {
                Debug.Log($"fluid_iir_filter_64 DISABLED {count} {q_lin}");
                return;
            }
            else
            {
                //Debug.Log($"fluid_iir_filter_64 {count} {q_lin}");
                /* IIR filter sample history */
                double dsp_hist1 = hist1;
                double dsp_hist2 = hist2;

                /* IIR filter coefficients */
                double dsp_a1 = a1;
                double dsp_a2 = a2;
                double dsp_b02 = b02;
                double dsp_b1 = b1;
                int dsp_fres_incr_count = fres_incr_count;
                int dsp_q_incr_count = q_incr_count;
                double dsp_fres = last_fres;
                double dsp_q = last_q;
                double dsp_fres_incr = fres_incr;
                double dsp_q_incr = q_incr;

                double dsp_centernode;
                int dsp_i;

                /* filter (implement the voice filter according to SoundFont standard) */

                /* Check for denormal number (too close to zero). */
                if (System.Math.Abs(dsp_hist1) < 1e-20d)
                    dsp_hist1 = 0.0d;    /* FIXME JMG - Is this even needed? */
                if (System.Math.Abs(dsp_hist2) < 1e-20d)
                    dsp_hist2 = 0.0d;

                for (dsp_i = 0; dsp_i < count; dsp_i++)
                {
                    /* The filter is implemented in Direct-II form. */
                    dsp_centernode = dsp_buf[dsp_i] - dsp_a1 * dsp_hist1 - dsp_a2 * dsp_hist2;
                    dsp_buf[dsp_i] = (float)(dsp_b02 * (dsp_centernode + dsp_hist2) + dsp_b1 * dsp_hist1);
                    dsp_hist2 = dsp_hist1;
                    dsp_hist1 = dsp_centernode;

                    if (dsp_fres_incr_count > 0 || dsp_q_incr_count > 0)
                    {
                        if (dsp_fres_incr_count > 0)
                        {
                            dsp_fres_incr_count--;
                            dsp_fres += dsp_fres_incr;
                        }

                        if (dsp_q_incr_count > 0)
                        {
                            dsp_q_incr_count--;
                            dsp_q += dsp_q_incr;
                            if (dsp_q < Q_MIN)
                            {
                                dsp_q_incr_count = 0;
                                dsp_q = Q_MIN;
                            }
                        }

                        fluid_iir_filter_calculate_coefficients(dsp_fres, dsp_q, out dsp_a1, out dsp_a2, out dsp_b02, out dsp_b1);
                    }
                }

                hist1 = dsp_hist1;
                hist2 = dsp_hist2;
                a1 = dsp_a1;
                a2 = dsp_a2;
                b02 = dsp_b02;
                b1 = dsp_b1;
                last_fres = dsp_fres;
                fres_incr_count = dsp_fres_incr_count;
                last_q = dsp_q;
                q_incr_count = dsp_q_incr_count;
            }
        }

        /* Macro for declaring an rvoice event function (#fluid_rvoice_function_t). The functions may only access
         * those params that were previously set in fluid_voice.c
         */
        //#define DECLARE_FLUID_RVOICE_FUNCTION(name) void name(void* obj, const fluid_rvoice_param_t param[MAX_EVENT_PARAMS])

        public override void fluid_iir_filter_init(fluid_iir_filter_type ptype, fluid_iir_filter_flags pflags)
        {
            type = ptype;
            flags = pflags;

            if (type != fluid_iir_filter_type.FLUID_IIR_DISABLED)
            {
                fluid_iir_filter_reset();
            }
        }

        public override void fluid_iir_filter_reset()
        {
            hist1 = 0d;
            hist2 = 0d;
            last_fres = -1d;
            q_lin = 0d;
            last_q = 0d;
            fres_incr = 0d;
            fres_incr_count = 0;
            q_incr = 0d;
            q_incr_count = 0;
            filter_startup = true;
        }

        public override void fluid_iir_filter_set_fres(float pfres)
        {
            //Debug.Log($"fluid_iir_filter_set_fres pfres:{pfres}");

            fres = pfres;
        }

        public override void fluid_iir_filter_set_q(float pq, float offset /*MPTK Specific*/)
        {
            double q = pq;
            //Debug.Log(pq + " " + offset);

            // MPTK: flags=FLUID_IIR_NOFLAGS -> 0
            //Debug.Log($"fluid_iir_filter_set_q q:{q} {flags}");

            if ((((int)flags & (int)fluid_iir_filter_flags.FLUID_IIR_Q_ZERO_OFF) != 0) && q <= 0d)
            {
                q = 0;
            }
            else if (((int)flags & (int)fluid_iir_filter_flags.FLUID_IIR_Q_LINEAR) != 0)
            {
                /* q is linear (only for user-defined filter)
                 * increase to avoid Q being somewhere between zero and one,
                 * which results in some strange amplified lowpass signal
                 */
                q++;
            }
            else
            {
                // Default with MPTK (and fluidsynth)
                q = fluid_iir_filter_q_from_dB(q, offset);
                //Debug.Log($"fluid_iir_filter_q_from_dB voice.q_dB:{pq} offset:{offset} -> q:{q}");
            }

            q_lin = q;

            if (filter_startup)
            {
                last_q = q;
                q_incr_count = 0;
            }
            else
            {
                if (q >= Q_MIN && last_q < Q_MIN)
                    last_q = Q_MIN;

                q_incr = (q - last_q) / FLUID_BUFSIZE;
                q_incr_count = FLUID_BUFSIZE;
            }
        }

        private static double fluid_iir_filter_q_from_dB(double q_dB, double offset /*MPTK specific*/)
        {
            /* The generator contains 'centibels' (1/10 dB) => divide by 10 to
             * obtain dB */
            q_dB /= 10f;

            /* Range: SF2.01 section 8.1.3 # 8 (convert from cB to dB => /10) */
            q_dB = System.Math.Clamp(q_dB + offset, 0d, 96d); // replaced by the following line to be compliant with Unity 2019.4

            /* Short version: Modify the Q definition in a way, that a Q of 0
             * dB leads to no resonance hump in the freq. response.
             *
             * Long version: From SF2.01, page 39, item 9 (initialFilterQ):
             * "The gain at the cutoff frequency may be less than zero when
             * zero is specified".  Assume q_dB=0 / q_lin=1: If we would leave
             * q as it is, then this results in a 3 dB hump slightly below
             * fc. At fc, the gain is exactly the DC gain (0 dB).  What is
             * (probably) meant here is that the filter does not show a
             * resonance hump for q_dB=0. In this case, the corresponding
             * q_lin is 1/sqrt(2)=0.707.  The filter should have 3 dB of
             * attenuation at fc now.  In this case Q_dB is the height of the
             * resonance peak not over the DC gain, but over the frequency
             * response of a non-resonant filter.  This idea is implemented as
             * follows: */
            q_dB -= 3.01d;

            /* The 'sound font' Q is defined in dB. The filter needs a linear
               q. Convert. */
            //Debug.Log($"fluid_iir_filter_q_from_dB q_dB:{q_dB} --> {Mathf.Pow(10f, q_dB / 20f)}");
            return System.Math.Pow(10f, q_dB / 20d);
        }

        private void fluid_iir_filter_calculate_coefficients(double current_fres, double current_q, out double output_a1, out double output_a2, out double output_b02, out double output_b1)
        {
            output_a1 = a1;
            output_a2 = a2;
            output_b02 = b02;
            output_b1 = b1;

            /* FLUID_IIR_Q_LINEAR may switch the filter off by setting Q==0 */
            if (current_q < Q_MIN)
            {
                return;
            }
            else
            {
                /*
                 * Those equations from Robert Bristow-Johnson's `Cookbook
                 * formulae for audio EQ biquad filter coefficients', obtained
                 * from Harmony-central.com / Computer / Programming. They are
                 * the result of the bilinear transform on an analogue filter
                 * prototype. To quote, `BLT frequency warping has been taken
                 * into account for both significant frequency relocation and for
                 * bandwidth readjustment'. */

                double omega = (2d * System.Math.PI) * (current_fres / output_rate);
                double sin_coeff = System.Math.Sin(omega);
                double cos_coeff = System.Math.Cos(omega);
                double alpha_coeff = sin_coeff / (2d * current_q);
                double a0_inv = 1d / (1d + alpha_coeff);
                double filter_gain = 1d;

                if (((int)flags & (int)fluid_iir_filter_flags.FLUID_IIR_NO_GAIN_AMP) == 0)
                {
                    filter_gain /= System.Math.Sqrt(current_q);
                }

                /* Calculate the filter coefficients. All coefficients are
                 * normalized by a0. Think of `a1' as `a1/a0'.
                 *
                 * Here a couple of multiplications are saved by reusing common expressions.
                 * The original equations should be:
                 *  b0=(1.-cos_coeff)*a0_inv*0.5*filter_gain;
                 *  b1=(1.-cos_coeff)*a0_inv*filter_gain;
                 *  b2=(1.-cos_coeff)*a0_inv*0.5*filter_gain; */

                /* "a" coeffs are same for all 3 available filter types */
                double a1_temp = -2d * cos_coeff * a0_inv;
                double a2_temp = (1d - alpha_coeff) * a0_inv;

                double b02_temp, b1_temp;

                switch (type)
                {
                    case fluid_iir_filter_type.FLUID_IIR_HIGHPASS:
                        //Debug.Log("fluid_iir_filter_calculate_coefficients FLUID_IIR_HIGHPASS");
                        b1_temp = (1f + cos_coeff) * a0_inv * filter_gain;
                        /* both b0 -and- b2 */
                        b02_temp = b1_temp * 0.5f;
                        b1_temp *= -1f;
                        break;

                    case fluid_iir_filter_type.FLUID_IIR_LOWPASS:
                        b1_temp = (1f - cos_coeff) * a0_inv * filter_gain;
                        /* both b0 -and- b2 */
                        b02_temp = b1_temp * 0.5d;
                        //Debug.Log("fluid_iir_filter_calculate_coefficients FLUID_IIR_LOWPASS");
                        break;

                    default:
                        /* filter disabled, should never get here */
                        return;
                }

                output_a1 = a1_temp;
                output_a2 = a2_temp;
                output_b02 = b02_temp;
                output_b1 = b1_temp;
            }
        }


        public override void fluid_iir_filter_calc(float output_rate, float fres_mod, float offset /*MPTK specific*/)
        {
            double fres;
            this.output_rate = output_rate;

            /* calculate the frequency of the resonant filter in Hz */
            fres = (double)fluid_conv.fluid_ct2hz(this.fres + fres_mod);
            //Debug.Log($"fluid_iir_filter_calc fres: {fres}  fres_mod: {fres_mod}  offset: {offset}" );
            fres += offset; //offset: MPTK specific

            /* FIXME - Still potential for a click during turn on, can we interpolate
               between 20khz cutoff and 0 Q? */

            /* I removed the optimization of turning the filter off when the
             * resonance frequence is above the maximum frequency. Instead, the
             * filter frequency is set to a maximum of 0.45 times the sampling
             * rate. For a 44100 kHz sampling rate, this amounts to 19845
             * Hz. The reason is that there were problems with anti-aliasing when the
             * synthesizer was run at lower sampling rates. Thanks to Stephan
             * Tassart for pointing me to this bug. By turning the filter on and
             * clipping the maximum filter frequency at 0.45*srate, the filter
             * is used as an anti-aliasing filter. */

            if (fres > 0.45d * output_rate)
            {
                fres = 0.45d * output_rate;
            }
            else if (fres < 5d)
            {
                fres = 5d;
            }

            /* if filter enabled and there is a significant frequency change.. */
            if (type != fluid_iir_filter_type.FLUID_IIR_DISABLED && System.Math.Abs(fres - last_fres) > 0.01d)
            {
                /* The filter coefficients have to be recalculated (filter
                 * parameters have changed). Recalculation for various reasons is
                 * forced by setting last_fres to -1.  The flag filter_startup
                 * indicates, that the DSP loop runs for the first time, in this
                 * case, the filter is set directly, instead of smoothly fading
                 * between old and new settings. */
                if (filter_startup)
                {
                    last_fres = fres;
                    fres_incr_count = 0;
                    filter_startup = last_q < Q_MIN;
                }
                else
                {
                    double fres_diff = fres - last_fres;
                    double num_buffers = last_q;
                    if (num_buffers < 1d)
                        num_buffers = 1d;
                    else if (num_buffers > 5d)
                        num_buffers = 5d;
                    double transition_samples = FLUID_BUFSIZE * num_buffers;
                    fres_incr = fres_diff / transition_samples;
                    fres_incr_count = (int)(transition_samples + 0.5d);
                }

                if (!filter_startup)
                {
                    fluid_iir_filter_calculate_coefficients(last_fres, last_q, out a1, out a2, out b02, out b1);
                }
            }
        }
    }
}
