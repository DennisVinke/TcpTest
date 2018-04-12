using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections;

public enum PlayerArrangement
{
    OneSideOfVX1 = 0,
    AroundVX1 = 1
};
/// <summary>  
///  VoxieCaptureVolume is the window through which objects will be reflected on the VX1. 
///  </summary>
///  <remarks>
///  This singleton gameobject handles connecting to the VX1, the list of drawable objects within the scene,
///  Game controllers, and handles adding / removing drawable objects.
///  Drawables are detected via collision, both as an ongoing check per update, and via collision
/// </remarks>  
[RequireComponent(typeof(Mesh))]
public class VoxieCaptureVolume : VoxieHelper
{
    // Controller Constants
    const int MAXCONTROLLERS = 4;

    [Tooltip("Scale capture volume (Higher values will cause content to appear smaller)")]
    [Range(1, 1000)]
    public uint Scale;

    [Tooltip("Will show capture volume of VX1 while emulating.\nWARNING: will cause animated meshes to 'twitch'")]
    public bool guidelines = false;

    [Tooltip("How players are arranged around VX1")]
    public PlayerArrangement PlayerStandingPosition = PlayerArrangement.OneSideOfVX1;

    private Vector3 AspectRatio;
    private Collider CollidingVolume;

    private voxie_wind_t vw = new voxie_wind_t();
    private voxie_frame_t vf = new voxie_frame_t();
    private voxie_inputs_t ins = new voxie_inputs_t();

    private Color32[] colorArray;

    private Vector3 currentPosition;
    // private Vector3 localScale;

    private List<Voxon.Component> drawable_components = new List<Voxon.Component>();

    // Controls
    protected struct xbox_input
    {
        public voxie_xbox_t input;
        public voxie_xbox_t offset;
        public voxie_xbox_t last_frame;
    }

    protected xbox_input[] controllers = new xbox_input[MAXCONTROLLERS]; 


    // Use this for initialization
    void Start()
    {
        Application.targetFrameRate = 30;

        // Set up our collision
        CollidingVolume = gameObject.GetComponent<Collider>();
        if (!CollidingVolume)
        {
            Debug.Log("VoxieCaptureVolume: No collider attached to GameObject. Generating Collider");
            gameObject.AddComponent<Collider>();
            CollidingVolume = gameObject.GetComponent<Collider>();
        }

        voxie_loadini_int(ref vw); // Load settings from our ini files.
        
        voxie_init(ref vw); //Start video and audio.

        voxie_setview(ref vf, -vw.aspx, -vw.aspy, -vw.aspz, vw.aspx, vw.aspy, vw.aspz); // Set out view dimentions

        // Start VX1 Loop
        StartCoroutine(VoxieUpdateLoop());

        // Anything that's required to be drawn?
        foreach (GameObject required_draw_object in GameObject.FindGameObjectsWithTag("VoxieDraw"))
        {
            required_draw_object.AddComponent<Voxon.GameObject>();
        }

        // Are there any new meshes to be added?
        OverlapCheck();

        // Set up controls
        for (int idx = 0; idx < MAXCONTROLLERS; ++idx)
        {
            controllers[idx].input = new voxie_xbox_t();
            controllers[idx].offset = new voxie_xbox_t();
            voxie_xbox_read(idx, ref controllers[idx].offset);
        }
    }

    /// <summary>  
    ///  This is our primary VX1 loop; we detach to ensure it will continue without blocking
    ///  </summary>
    public IEnumerator VoxieUpdateLoop()
    {
        // Base loop; provide ins to collect input for the frame
        while (voxie_breath(ref ins) == 0)
        {
            voxie_frame_start(ref vf);

            //draw wireframe box (provides destinct borders) but causes animated meshes to twitch
            if (guidelines)
            {
                voxie_drawbox(ref vf, -vw.aspx + 1e-3f, -vw.aspy + 1e-3f, -vw.aspz, +vw.aspx - 1e-3f, +vw.aspy - 1e-3f, +vw.aspz, 1, 0xffffff);
            }
            
            // Clear out any destroyed drawables (note: this is the drawable component not gameobject itself)
            drawable_components.RemoveAll(item => item == null);

            foreach (Voxon.Component comp in drawable_components)
            {
                comp.DrawMesh(ref vf, gameObject.transform);
            }

            // hasChanged warrents an update for all included meshes; after an update reset our changed status
            transform.hasChanged = false;

            voxie_frame_end();
            voxie_getvw(ref vw);

            // Ensure we keep to a sensible frame rate
            yield return new WaitForEndOfFrame();
        }

        Debug.Log("Voxie Quit. Code: " + voxie_breath(ref ins));

        Application.Quit();

    }

    public void OnApplicationQuit()
    {
        // Ensure All VX1 processes have finished before stopping program
        voxie_quitloop();
        voxie_uninit_int(0);
    }

    public void Update()
    {
        // EXAMPLE QUIT: Remove me once your binding system has been added.
        if(Voxon.Input.GetButtonDown("Quit") || Voxon.Input.GetKeyDown("Quit"))
        {
            Debug.Log("Quit via button");
            Application.Quit();
        }

        // Update our controller input details
        for (int idx = 0; idx < MAXCONTROLLERS; ++idx)
        {
            controllers[idx].last_frame = controllers[idx].input;
            voxie_xbox_read(idx, ref controllers[idx].input);
        }

        // Check if any new items entered frame
        OverlapCheck();

    }

    public void AddComponent(Voxon.Component comp)
    {
        if (!drawable_components.Contains(comp))
        {
            drawable_components.Add(comp);
        }
    }

    /// <summary>  
    ///  Check for colliders within the Capture volume. Required for any objects spawned within the capture volume (such as projectiles, or just loaded objects)
    ///  </summary>
    void OverlapCheck()
    {

        Collider[] colliders;

        if ((colliders = Physics.OverlapBox(transform.position, transform.localScale, transform.rotation)).Length > 0) //Presuming the object you are testing also has a collider 0 otherwise
        {
            foreach (var collider in colliders)
            {
                var go = collider.transform.root.gameObject; //This is the game object you collided with

                if (go.gameObject.tag == "VoxieHide" || go.GetComponent<Voxon.GameObject>() || !go.activeInHierarchy)
                {
                    continue;
                }
                else
                {
                    go.AddComponent<Voxon.GameObject>();
                }
                
            }
        }
    }

    // Collision event triggers
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Voxon.GameObject>())
        {
            other.gameObject.GetComponent<Voxon.GameObject>().Set_Degen(false);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<Voxon.GameObject>())
        {
            other.gameObject.GetComponent<Voxon.GameObject>().Set_Degen(true);
        }
    }

    // Input Functions (handle Controller IO)
    public short GetButtons(int player)
    {
        return controllers[player].input.but;
    }

    public short GetOldButtons(int player)
    {
        return controllers[player].last_frame.but;
    }

    public float GetAxis(int axis, int player)
    {
        if(axis == 1)
        {
            return Convert.ToSingle(controllers[player].input.lt) / 0x7FFF;
        }
        else if (axis == 2)
        {
            return Convert.ToSingle(controllers[player].input.rt) / 0x7FFF;
        }
        else if (PlayerStandingPosition == PlayerArrangement.OneSideOfVX1)
        {
            switch (axis)
            {
                case 3: // LeftStickX
                     return -1 * (Convert.ToSingle(controllers[player].input.tx0) / 0x7FFF);
                case 4: // LeftStickY
                    return Convert.ToSingle(controllers[player].input.ty0) / 0x7FFF;
                case 5: // RightStickX
                    return -1 * (Convert.ToSingle(controllers[player].input.tx1) / 0x7FFF);
                case 6: // RightStickY
                    return Convert.ToSingle(controllers[player].input.ty1) / 0x7FFF;
                default:
                    return 0;
            }
        }
        else
        {
            switch (axis)
            {
                case 3: // LeftStickX
                    switch (player)
                    {
                        case 0:
                            return -1 * (Convert.ToSingle(controllers[player].input.tx0) / 0x7FFF);
                        case 1:
                            return (Convert.ToSingle(controllers[player].input.tx0) / 0x7FFF);
                        case 2:
                            return -1 * (Convert.ToSingle(controllers[player].input.ty0) / 0x7FFF);
                        case 3:
                            return (Convert.ToSingle(controllers[player].input.ty0) / 0x7FFF);
                        default:
                            break;
                    }
                    break;
                case 4: // LeftStickY
                    switch (player)
                    {
                        case 0:
                            return (Convert.ToSingle(controllers[player].input.ty0) / 0x7FFF);
                        case 1:
                            return -1 * (Convert.ToSingle(controllers[player].input.ty0) / 0x7FFF);
                        case 2:
                            return (Convert.ToSingle(controllers[player].input.tx0) / 0x7FFF);
                        case 3:
                            return -1 * (Convert.ToSingle(controllers[player].input.tx0) / 0x7FFF);
                        default:
                            break;
                    }
                    break;
                case 5: // RightStickX
                    switch (player)
                    {
                        case 0:
                            return -1 * (Convert.ToSingle(controllers[player].input.tx1) / 0x7FFF);
                        case 1:
                            return (Convert.ToSingle(controllers[player].input.tx1) / 0x7FFF);
                        case 2:
                            return -1 * (Convert.ToSingle(controllers[player].input.ty1) / 0x7FFF);
                        case 3:
                            return (Convert.ToSingle(controllers[player].input.ty1) / 0x7FFF);
                        default:
                            break;
                    }
                    break;
                case 6: // RightStickY
                    switch (player)
                    {
                        case 0:
                            return (Convert.ToSingle(controllers[player].input.ty1) / 0x7FFF);
                        case 1:
                            return -1 * (Convert.ToSingle(controllers[player].input.ty1) / 0x7FFF);
                        case 2:
                            return (Convert.ToSingle(controllers[player].input.tx1) / 0x7FFF);
                        case 3:
                            return -1 * (Convert.ToSingle(controllers[player].input.tx1) / 0x7FFF);
                        default:
                            break;
                    }
                    break;
                default:
                    return 0;
            }
            return 0;
        }
    }

    // Editor Functions
    [ExecuteInEditMode]
    void OnEnable()
    {
        // We don't want to send this mesh to voxie
        gameObject.GetComponent<Collider>().tag = "VoxieHide";

        // Load Voxie aspect ratio details
        voxie_loadini_int(ref vw);

        AspectRatio = new Vector3(vw.aspx, vw.aspz, vw.aspy);

        transform.localScale = AspectRatio * Scale;
    }

    void OnValidate()
    {
        // Load Voxie aspect ratio details
        voxie_loadini_int(ref vw);

        AspectRatio = new Vector3(vw.aspx, vw.aspz, vw.aspy);

        transform.localScale = AspectRatio * Scale;
    }
}
