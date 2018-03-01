using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 *  All of this FFT code has been ported to C# from the Java version of the code
 *  found at this link: https://github.com/skooter500/matt2/blob/master/PhD/src/matt/dsp/FastFourierTransform.java
 * 
 **/

static class FastFourierTransform
{

    private static int n, nu;

    private static int Bitrev(int j)
    {

        int j2;
        int j1 = j;
        int k = 0;
        for (int i = 1; i <= nu; i++)
        {
            j2 = j1 / 2;
            k = 2 * k + j1 - 2 * j2;
            j1 = j2;
        }
        return k;
    }

    public static float[] FftMag(float[] x)
    {
        return FftMag(x, 0, x.Length);
    }

    public static float[] FftLogMag(float[] x)
    {
        return FftLogMag(x, 0, x.Length);
    }


    public static float[] FftMag(float[] x, int start, int length)
    {
        // assume n is a power of 2
        n = length;
        nu = (int)(Mathf.Log(n) / Mathf.Log(2));
        int n2 = n / 2;
        int nu1 = nu - 1;
        float[] xre = new float[n];
        float[] xim = new float[n];
        float[] mag = new float[n2];
        float tr, ti, p, arg, c, s;
        for (int i = 0; i < n; i++)
        {
            xre[i] = x[i + start];
            xim[i] = 0.0f;
        }
        int k = 0;

        for (int l = 1; l <= nu; l++)
        {
            while (k < n)
            {
                for (int i = 1; i <= n2; i++)
                {
                    p = Bitrev(k >> nu1);
                    arg = 2 * (float)Mathf.PI * p / n;
                    c = (float)Mathf.Cos(arg);
                    s = (float)Mathf.Sin(arg);
                    tr = xre[k + n2] * c + xim[k + n2] * s;
                    ti = xim[k + n2] * c - xre[k + n2] * s;
                    xre[k + n2] = xre[k] - tr;
                    xim[k + n2] = xim[k] - ti;
                    xre[k] += tr;
                    xim[k] += ti;
                    k++;
                }
                k += n2;
            }
            k = 0;
            nu1--;
            n2 = n2 / 2;
        }
        k = 0;
        int r;
        while (k < n)
        {
            r = Bitrev(k);
            if (r > k)
            {
                tr = xre[k];
                ti = xim[k];
                xre[k] = xre[r];
                xim[k] = xim[r];
                xre[r] = tr;
                xim[r] = ti;
            }
            k++;
        }

        mag[0] = (float)(Mathf.Sqrt(xre[0] * xre[0] + xim[0] * xim[0])) / n;
        for (int i = 1; i < n / 2; i++)
            mag[i] = 2 * (float)(Mathf.Sqrt(xre[i] * xre[i] + xim[i] * xim[i])) / n;
        return mag;
    }

    public static float[] FftLogMag(float[] x, int start, int length)
    {
        // assume n is a power of 2
        n = length;
        nu = (int)(Mathf.Log(n) / Mathf.Log(2));
        int n2 = n / 2;
        int nu1 = nu - 1;
        float[] xre = new float[n];
        float[] xim = new float[n];
        float[] mag = new float[n2];
        float tr, ti, p, arg, c, s;
        for (int i = 0; i < n; i++)
        {
            xre[i] = (float)Mathf.Log((float)x[i + start]);
            xim[i] = 0.0f;
        }
        int k = 0;

        for (int l = 1; l <= nu; l++)
        {
            while (k < n)
            {
                for (int i = 1; i <= n2; i++)
                {
                    p = Bitrev(k >> nu1);
                    arg = 2 * (float)Mathf.PI * p / n;
                    c = (float)Mathf.Cos(arg);
                    s = (float)Mathf.Sin(arg);
                    tr = xre[k + n2] * c + xim[k + n2] * s;
                    ti = xim[k + n2] * c - xre[k + n2] * s;
                    xre[k + n2] = xre[k] - tr;
                    xim[k + n2] = xim[k] - ti;
                    xre[k] += tr;
                    xim[k] += ti;
                    k++;
                }
                k += n2;
            }
            k = 0;
            nu1--;
            n2 = n2 / 2;
        }
        k = 0;
        int r;
        while (k < n)
        {
            r = Bitrev(k);
            if (r > k)
            {
                tr = xre[k];
                ti = xim[k];
                xre[k] = xre[r];
                xim[k] = xim[r];
                xre[r] = tr;
                xim[r] = ti;
            }
            k++;
        }
        mag[0] = (float)(Mathf.Sqrt(xre[0] * xre[0] + xim[0] * xim[0])) / n;
        for (int i = 1; i < n / 2; i++)
            mag[i] = 2 * (float)(Mathf.Sqrt(xre[i] * xre[i] + xim[i] * xim[i])) / n;
        return mag;
    }

    /**
     * Calcuate the nearest power of 2 to a number 
     */
    public static int smallestPowerOf2(int value)
    {
        int i = 0;
        float nearest = 0;

        while (value > nearest)
        {
            i++;
            nearest = (float)Mathf.Pow(2.0f, i);

        }
        return i - 1;
    }



    public static void PrintFft(float[] fft, float sampleRate)
    {
        float binWidth = fft.Length / sampleRate;
        Debug.Log("Frame size: " + fft.Length);
        for (int i = 0; i < fft.Length; i++)
        {
            Debug.Log(((float)i) * binWidth + "\t" + fft[i]);
        }
    }
}
