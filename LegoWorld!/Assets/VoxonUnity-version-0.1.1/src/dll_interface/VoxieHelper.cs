using System;
using System.Runtime.InteropServices;
using System.Reflection;
using UnityEngine;

/// <summary>  
///  VoxieHelper is a collection of VX1 dll specific structures and DLL 
///  function bindings
///  </summary>
public class VoxieHelper : MonoBehaviour
    {

        protected struct voxie_xbox_t
        { 
            public short but, lt, rt, tx0, ty0, tx1, ty1;
        }

        protected struct point2d
        {
            public float x, y;
        }

        protected struct voxie_wind_t
        {
        //The following fields can be updated on later calls to voxie_init():
        public double freq;            //starting value in Hz (must be set before first call to voxie_init())

        public double phase;           //phase lock (0.0-1.0) (can be updated on later calls to voxie_init())
        public double emuhang;         //emulator horizontal angle (radians)
        public double emuvang;         //emulator vertical   angle (radians)
        public double emudist;         //emulator distance; minimum is 2000.0.

        public point2d proj0, proj1, proj2, proj3, proj4, proj5, proj6, proj7;       //settings for quadrilateral (keystone) compensation (use voxiedemo mode 2 to calibrate)

        public float aspx, aspy, aspz; //aspect ratio, loaded from voxiebox.ini, used by application. Values typically around 1.f

        public float gamma;            //gamma value for interpolating in voxie_drawspr()/voxie_drawheimap().
        public float density;          //scale factor controlling dot density of STL model rendering (default:1.f)

        //The following fields must be set before the first call of voxie_init():
        public int useemu;             //0=Voxiebox, 1=emulation (projection on 2D display)

        public int excl_mouse;

        public int voxie_vid;          //display index of projector. (Right-click Desktop..Screen Resolution..
                                       //   Multiple Displays..Extend these displays)
        public int voxie_aud;          //audio channel index of motor. (Playback devices..Speakers..Configure..
                                       //   Audio Channels)
        public int sndfx_aud0, sndfx_aud1;       //audio channel indices of sound effects, [0]=left, [1]=right

        public int projrate;           //projector rate in Hz (60..107)
        public int framepervol;        //# projector frames per volume (1-16). Ex:framepervol=3 at projrate=60
                                       //gives 60/3 = 20Hz volume rate
        public int playsamprate;       //sample rate used by audio driver (written by voxie_init()). 0 is written
                                       //   if no audio channels are enabled in voxiebox.ini.
        public int playnchans;         //number of audio channels expected to be rendered by user audio mixer
        public int recsamprate;        //recording sample rate - to use, must write before voxie_init()
        public int recnchans;          //number of audio channels in recording callback

        //The following fields can be updated on successive calls to voxie_init():
        public int usecol;             //0=mono white, 1=full color time multiplexed,
                                       //-1=red, -2=green, -3=yellow, -4=blue, -5=magenta, -6=cyan
        public int drawstroke;         //1=draw on up stroke, 2=draw on down stroke, 3=draw on both up&down
        public int dither;             //0=dither off, 1=dither mono only, 2=dither RGB only, 3=dither both
        public int smear;              //1+=increase brightness in x-y at post processing (slower render), 0=not

        public int mono_r, mono_g, mono_b; // White intensity (r,g,b) 

        public int xdim, ydim;         //projector dimensions (912,1140)
        public int voxie_vol;          //amplitude scale when using sine wave audio output (0-100)
        public int sndfx_vol;          //amplitude scale of sound effects (0-100)


        public int hwsync_frame0, hwsync_phase;
        public int hwsync_amp0, hwsync_amp1, hwsync_amp2, hwsync_amp3;
        public int hwsync_pha0, hwsync_pha1, hwsync_pha2, hwsync_pha3;
        public int hwsync_sensmode, hwsync_levthresh; //can be modified later
        public int usekeystone;        //enable used of quadrilateral compensation (proj[0..7])

        public int colo_r, colo_g, colo_b; // Color intensity (r,g,b) 

        public int HighLumenDangerProtect; //if enabled (nonzero), protects ledr/g/b from going too high
        public int menu_on_voxie;      //1=display menu on voxiebox view

        public int excl_audio;         //1=exclusive audio mode (much faster & more stable sync - recommended!
                                       //0=shared audio mode (if audio access to other programs required)

        public int projctrl;
        public int mirrorx, mirrory;   //projector hardware flipping (I suggest avoiding this)
        public int flip;               //flip coordinate system in voxiebox.dll
        public int reserved;

        // public int lc45ctrl;           //0=I2C to projector (not fully supported), 1=USB to projector (default)
    }

        public struct voxie_frame_t
        {
            public IntPtr f, p, fp;
            private int x, y, usecol, drawplanes, x0, y0, x1, y1;
            public float xmul, ymul, zmul, xadd, yadd, zadd;
        }

        public struct point3d
        {
            public float x, y, z;
        }

        public struct point3dcol_t
        {
            public float x, y, z;
            public int col;
        }

        public struct pol_t
        {
            public float x, y, z;
            public int p2;
        }

        public struct poltex_t
        {
            public float x, y, z, u, v;
            public int col;
        }

        public struct tiletype
        {
            public IntPtr first_pixel;          // pointer to first pixel of the texture (usually the top-left corner)
            public Int64 pitch;                   // pitch, or number of bytes per horizontal line (usually x*4)
            public Int64 height, width;          // width & height of texture
        }

        protected struct voxie_inputs_t
        {
            public int bstat, obstat, dmousx, dmousy, dmousz;
        }

        protected struct tri_t
        {
            public float x0, y0, z0;
            public int n0;
            public float x1, y1, z1;
            public int n1;
            public float x2, y2, z2;
            public int n2;
        }

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_load(ref voxie_wind_t vw);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_loadini_int(ref voxie_wind_t vw);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_getvw(ref voxie_wind_t vw);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_init(ref voxie_wind_t vw);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_uninit_int(int code);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_breath(ref voxie_inputs_t ins);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_quitloop();

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern double voxie_klock();

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_keystat(int i);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_keyread();

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_doscreencap();

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_setview(ref voxie_frame_t vf, float x0, float y0, float z0, float x1, float y1,
            float z1);
    
        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_frame_start(ref voxie_frame_t vf);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_frame_end();

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_setleds(int r, int g, int b);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawvox(ref voxie_frame_t vf, float fx, float fy, float fz, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawbox(ref voxie_frame_t vf, float x0, float y0, float z0, float x1, float y1,
            float z1, int fillmode, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawlin(ref voxie_frame_t vf, float x0, float y0, float z0, float x1, float y1,
            float z1, int col);

#if !FIX_AMD64_HACK
        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawpol(ref voxie_frame_t vf, ref pol_t pt, int n, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawmesh(ref voxie_frame_t vf, point3dcol_t[] vt, int vtn, int[] mesh, int meshn, int fillmode, int col);

    // voxie_drawmeshtex() also supports rendering from a texture in memory. To do so, do the following:
    // Add(1<<3) to the flags parameter
    // texnam(the 2nd parameter) is now treated as a pointer to a tiletype structure(tiletype*) instead of a filename string.
    // All textures must use 32-bit ARGB color format - with blue as least significant.
    [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawmeshtex(ref voxie_frame_t vf, ref tiletype texture, poltex_t[] vt, int vtn, int[] mesh, int meshn,
            int flags, int col);

    [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
    protected static extern void voxie_drawmeshtex(ref voxie_frame_t vf, int nullptr, poltex_t[] vt, int vtn, int[] mesh, int meshn,
            int flags, int col);
#else
	[DllImport("voxiebox",CallingConvention=CallingConvention.Cdecl)] protected extern static void   voxie_drawpol    (ref voxie_frame_t vf, ref tri_t tri, int n, int col);
	[DllImport("voxiebox",CallingConvention=CallingConvention.Cdecl)] protected extern static void   voxie_drawmesh   (ref voxie_frame_t vf, ref point3d pt, int ptn, ref int mesh, int meshn, int fillmode, int col);
#endif

    [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawsph(ref voxie_frame_t vf, float fx, float fy, float fz, float rad,
            int issol, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawcone(ref voxie_frame_t vf, float x0, float y0, float z0, float r0, float x1,
            float y1, float z1, float r1, int issol, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_drawspr(ref voxie_frame_t vf, [MarshalAs(UnmanagedType.LPStr)] string st,
            ref point3d p, ref point3d r, ref point3d d, ref point3d f, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_printalph(ref voxie_frame_t vf, ref point3d p, ref point3d r, ref point3d d,
            int col, [MarshalAs(UnmanagedType.LPStr)] string st);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_drawcube(ref voxie_frame_t vf, ref point3d p, ref point3d r, ref point3d d,
            ref point3d f, int fillmode, int col);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern float voxie_drawheimap(ref voxie_frame_t vf, [MarshalAs(UnmanagedType.LPStr)] string st,
            ref point3d p, ref point3d r, ref point3d d, ref point3d f, int colorkey, int heimin, int flags);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_playsound([MarshalAs(UnmanagedType.LPStr)] string st, int chan, int volperc0,
            int volperc1, float frqmul);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern int voxie_xbox_read (int id, ref voxie_xbox_t vx);

        [DllImport("voxiebox", CallingConvention = CallingConvention.Cdecl)]
        protected static extern void voxie_xbox_write (int id, float lmot, float rmot);
}
