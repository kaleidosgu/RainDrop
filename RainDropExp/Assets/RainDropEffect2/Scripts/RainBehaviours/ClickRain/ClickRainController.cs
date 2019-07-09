using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using RainDropEffect;

public class ClickRainController : MonoBehaviour
{
    public ClickRainVariables Variables { get; set; }
    [HideInInspector]
    public int RenderQueue { get; set; }
    public Camera camera { get; set; }
    public float Alpha { get; set; }
	public Vector2 GlobalWind { get; set; }
    public Vector3 GForceVector { get; set; }
    public bool NoMoreRain { get; set; }
    public RainDropTools.RainDropShaderType ShaderType { get; set; }

    private int oldSpawnLimit = 0;
    private bool isOneShot = false;
    private float oneShotTimeleft = 0f;
    private float timeElapsed = 0f;
    private float interval = 0f;
    private bool isWaitingDelay = false;

    private float m_fUpTime;
    private Camera m_camera;
    private void Start()
    {
        //m_camera = Camera.main;
        //m_camera = transform.parent.parent.GetComponent<Camera>();
    }
    public bool IsPlaying
    {
        get
        {
            return drawers.FindAll(t => t.currentState == DrawState.Disabled).Count != drawers.Count;
        }
    }

    public enum DrawState
    {
        Playing,
        Disabled,
    }

    [System.Serializable]
    public class ClickRainDrawerContainer : RainDrawerContainer<RainDrawer>
    {
        public DrawState currentState = DrawState.Disabled;
        public Vector3 startSize;
        public Vector3 startPos;
        public float TimeElapsed = 0f;
        public float lifetime = 0f;

        public ClickRainDrawerContainer(string name, Transform parent) : base(name, parent) { }
    }

    public List<ClickRainDrawerContainer> drawers = new List<ClickRainDrawerContainer>();

    /// <summary>
    /// Refresh this instance.
    /// </summary>

    public void Refresh()
    {
        foreach (var d in drawers)
        {
            d.Drawer.Hide();
            DestroyImmediate(d.Drawer.gameObject);
        }

        drawers.Clear();

        for (int i = 0; i < Variables.MaxRainSpawnCount; i++)
        {
            ClickRainDrawerContainer container = new ClickRainDrawerContainer("Click RainDrawer " + i, this.transform);
            container.currentState = DrawState.Disabled;
            drawers.Add(container);
        }
    }

    /// <summary>
    /// Play this instance.
    /// </summary>
    public void Play()
    {
        StartCoroutine(PlayDelay(Variables.Delay));
        
    }

    IEnumerator PlayDelay(float delay)
    {
        float t = 0f;
        while (t <= delay)
        {
            isWaitingDelay = true;
            t += Time.deltaTime;
            yield return null;
        }
        isWaitingDelay = false;

        if (drawers.Find(x => x.currentState == DrawState.Playing) != null)
        {
            yield break;
        }

        //for (int i = 0; i < drawers.Count; i++)
        //{
        //    InitializeDrawer(drawers[i]);
        //    drawers[i].currentState = DrawState.Disabled;
        //}

        isOneShot = Variables.PlayOnce;
        if (isOneShot)
        {
            oneShotTimeleft = Variables.Duration;
        }

        yield break;
    }

    /// <summary>
    /// Update.
    /// </summary>
    public void UpdateController()
    {
        if (Variables == null)
        {
            return;
        }
        
        if( Input.GetMouseButtonDown(0) == true)
        {
            m_fUpTime = Time.time;
        }

        if(Input.GetMouseButtonUp(0) == true)
        {
            float fTimeDiff = Time.time - m_fUpTime;
            Spawn(fTimeDiff);
        }

        for (int i = 0; i < drawers.Count(); i++)
        {
            UpdateInstance(drawers[i], i);
        }
    }
    
    private void Spawn(float fTimeFromDownToUp)
    {
        var spawnRain = drawers.Find(x => x.currentState == DrawState.Disabled);
        if (spawnRain == null)
        {
            //Debug.LogError ("Spawn limit!");
            return;
        }

        //InitializeDrawer(spawnRain,new Vector2(0,0));
        float fTimeRate = 0.0f;
        if(Variables.TimeOfMaxSizeRainDrop > 0)
        {
            fTimeRate = fTimeFromDownToUp / Variables.TimeOfMaxSizeRainDrop;
        }
        if(fTimeRate > 1.0f)
        {
            fTimeRate = 1.0f;
        }
        InitializeDrawer(spawnRain, fTimeRate);
        spawnRain.currentState = DrawState.Playing;
    }


    private float GetProgress(ClickRainDrawerContainer dc)
    {
        return dc.TimeElapsed / dc.lifetime;
    }


    private void InitializeDrawer(ClickRainDrawerContainer dc,float fTimeRate)
    {
        dc.TimeElapsed = 0f;
        dc.lifetime = RainDropTools.Random(Variables.LifetimeMin, Variables.LifetimeMax);
        //dc.transform.localPosition = RainDropTools.GetSpawnLocalPos(this.transform, camera, 0f, Variables.SpawnOffsetY);

        Vector3 vecMouseWorld = camera.ScreenToWorldPoint(Input.mousePosition);
        vecMouseWorld = new Vector3(vecMouseWorld.x, vecMouseWorld.y - transform.position.y, 0);

        dc.transform.localPosition = vecMouseWorld;
        dc.startPos = dc.transform.localPosition;
        float fSizeX = Variables.SizeMaxX * fTimeRate;
        float fSizeY = Variables.SizeMaxY * fTimeRate;
        if (fSizeX < Variables.SizeMinX)
        {
            fSizeX = Variables.SizeMinX;
        }
        if( fSizeY < Variables.SizeMinY)
        {
            fSizeY = Variables.SizeMinY;
        }
        dc.startSize = new Vector3(fSizeX, fSizeY, 1f);
        dc.transform.localEulerAngles += Vector3.forward * (Variables.AutoRotate ? UnityEngine.Random.Range(0f, 179.9f) : 0f);
        dc.Drawer.NormalMap = Variables.NormalMap;
        dc.Drawer.ReliefTexture = Variables.OverlayTexture;
        dc.Drawer.Darkness = Variables.Darkness;
        dc.Drawer.Hide();
    }


    private void UpdateShader(ClickRainDrawerContainer dc, int index)
    {
        float progress = GetProgress(dc);
        dc.Drawer.RenderQueue = RenderQueue + index;
        dc.Drawer.NormalMap = Variables.NormalMap;
        dc.Drawer.ReliefTexture = Variables.OverlayTexture;
        dc.Drawer.OverlayColor = new Color(
            Variables.OverlayColor.r,
            Variables.OverlayColor.g,
            Variables.OverlayColor.b,
            Variables.OverlayColor.a * Variables.AlphaOverLifetime.Evaluate(progress) * Alpha
        );
        //dc.Drawer.DistortionStrength = Variables.DistortionValue * Variables.DistortionOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.DistortionStrength = Variables.DistortionValue * 0.5f * Alpha;
        dc.Drawer.ReliefValue = Variables.ReliefValue * Variables.ReliefOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.Blur = Variables.Blur * Variables.BlurOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.Darkness = Variables.Darkness * Alpha;
        dc.transform.localScale = dc.startSize * Variables.SizeOverLifetime.Evaluate(progress);
        // old
        //dc.transform.localPosition = dc.startPos + Vector3.up * Variables.PosYOverLifetime.Evaluate(progress);
        Vector3 gforced = RainDropTools.GetGForcedScreenMovement(this.camera.transform, this.GForceVector);
        gforced = gforced.normalized;
        float fSizeRate = dc.startSize.x / Variables.SizeMaxX;
        if( fSizeRate >= Variables.GForceEffectSizeRate )
        {
            float fOverLifeValue = Variables.PosYOverLifetime.Evaluate(progress);
            //Debug.Log(string.Format("fOverLifeValue = [{0}]", fOverLifeValue));
            //dc.transform.localPosition += new Vector3(-gforced.x * fSizeRate, -gforced.y * fSizeRate, 0f) * 0.01f * fOverLifeValue;
            dc.transform.localPosition += new Vector3(-gforced.x * fSizeRate, -gforced.y * fSizeRate, 0f) * -0.01f ;
        }
        dc.transform.localPosition += progress * new Vector3(GlobalWind.x, GlobalWind.y, 0f);
        dc.transform.localPosition = new Vector3(dc.transform.localPosition.x, dc.transform.localPosition.y, 0f);
        dc.Drawer.ShaderType = this.ShaderType;
        dc.Drawer.Show();
    }


    /// <summary>
    /// Update rain variables
    /// </summary>
    /// <param name="i">The index.</param>
    private void UpdateInstance(ClickRainDrawerContainer dc, int index)
    {
        if (dc.currentState == DrawState.Playing)
        {
            if (GetProgress(dc) >= 1.0f && (Variables.ExistAllTime == false))
            {
                dc.Drawer.Hide();
                dc.currentState = DrawState.Disabled;
            }
            else
            {
                dc.TimeElapsed += Time.deltaTime;
                UpdateShader(dc, index);
            }
        }
    }
}
