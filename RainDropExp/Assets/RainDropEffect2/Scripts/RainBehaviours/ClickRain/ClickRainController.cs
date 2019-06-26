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

        CheckSpawnNum();

        if(Input.GetMouseButtonUp(0) == true)
        {
            Spawn();
        }

        //if (NoMoreRain)
        //{
        //    timeElapsed = 0f;
        //}
        //else if (isOneShot)
        //{
        //    oneShotTimeleft -= Time.deltaTime;
        //    if (oneShotTimeleft > 0f)
        //    {
        //        CheckSpawnTime();
        //    }
        //}
        //else if (!isWaitingDelay)
        //{
        //    CheckSpawnTime();
        //}

        for (int i = 0; i < drawers.Count(); i++)
        {
            UpdateInstance(drawers[i], i);
        }
    }


    private void CheckSpawnNum()
    {
        //int diff = Variables.MaxRainSpawnCount - drawers.Count();

        //// MaxRainSpawnCount was increased
        //if (diff > 0)
        //{
        //    for (int i = 0; i < diff; i++)
        //    {
        //        ClickRainDrawerContainer container = new ClickRainDrawerContainer("Click RainDrawer " + (drawers.Count() + i), this.transform);
        //        container.currentState = DrawState.Disabled;
        //        drawers.Add(container);
        //    }
        //}

        //// MaxRainSpawnCount was decreased
        //if (diff < 0)
        //{
        //    int rmcnt = -diff;
        //    List<ClickRainDrawerContainer> removeList = drawers.FindAll(x => x.currentState != DrawState.Playing).Take(rmcnt).ToList();
        //    if (removeList.Count() < rmcnt)
        //    {
        //        removeList.AddRange(drawers.FindAll(x => x.currentState == DrawState.Playing).Take(rmcnt - removeList.Count()));
        //    }

        //    foreach (var rem in removeList)
        //    {
        //        rem.Drawer.Hide();
        //        DestroyImmediate(rem.Drawer.gameObject);
        //    }

        //    drawers.RemoveAll(x => x.Drawer == null);
        //}
    }


    private void CheckSpawnTime()
    {
		if (interval == 0f) 
		{
			interval = Variables.Duration / RainDropTools.Random(Variables.EmissionRateMin, Variables.EmissionRateMax);
		}

		timeElapsed += Time.deltaTime;
		if (timeElapsed >= interval)
		{
			int spawnNum = (int) Mathf.Min ((timeElapsed / interval), Variables.MaxRainSpawnCount - drawers.FindAll (x => x.currentState == DrawState.Playing).Count ());
			for (int i = 0; i < spawnNum; i++)
			{
				Spawn();
			}
			interval = Variables.Duration / RainDropTools.Random(Variables.EmissionRateMin, Variables.EmissionRateMax);
			timeElapsed = 0f;
		}
    }


    private void Spawn()
    {
        var spawnRain = drawers.Find(x => x.currentState == DrawState.Disabled);
        if (spawnRain == null)
        {
            //Debug.LogError ("Spawn limit!");
            return;
        }

        //InitializeDrawer(spawnRain,new Vector2(0,0));
        InitializeDrawer(spawnRain);
        spawnRain.currentState = DrawState.Playing;
    }


    private float GetProgress(ClickRainDrawerContainer dc)
    {
        return dc.TimeElapsed / dc.lifetime;
    }


    private void InitializeDrawer(ClickRainDrawerContainer dc)
    {
        dc.TimeElapsed = 0f;
        dc.lifetime = RainDropTools.Random(Variables.LifetimeMin, Variables.LifetimeMax);
        //dc.transform.localPosition = RainDropTools.GetSpawnLocalPos(this.transform, camera, 0f, Variables.SpawnOffsetY);
        Vector3 vecPos = RainDropTools.GetSpawnLocalPos(this.transform, camera, 0f, Variables.SpawnOffsetY);

        Vector3 vecMouseWorld = camera.ScreenToWorldPoint(Input.mousePosition);
        vecMouseWorld = new Vector3(vecMouseWorld.x, vecMouseWorld.y + 2000, 0);
        Debug.Log(string.Format("mouse [{0}], world [{1}] vecPos[{2}]", Input.mousePosition, vecMouseWorld, vecPos));
        //Vector2 vecPos = new Vector2(vecMouseWorld.x - transform.position.x, vecMouseWorld.y - transform.position.y);

        dc.transform.localPosition = vecMouseWorld;
        dc.startPos = dc.transform.localPosition;
        dc.startSize = new Vector3(
            RainDropTools.Random(Variables.SizeMinX, Variables.SizeMaxX),
            RainDropTools.Random(Variables.SizeMinY, Variables.SizeMaxY),
            1f
        );
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
        dc.Drawer.DistortionStrength = Variables.DistortionValue * Variables.DistortionOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.ReliefValue = Variables.ReliefValue * Variables.ReliefOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.Blur = Variables.Blur * Variables.BlurOverLifetime.Evaluate(progress) * Alpha;
        dc.Drawer.Darkness = Variables.Darkness * Alpha;
        dc.transform.localScale = dc.startSize * Variables.SizeOverLifetime.Evaluate(progress);
        // old
        //dc.transform.localPosition = dc.startPos + Vector3.up * Variables.PosYOverLifetime.Evaluate(progress);
        Vector3 gforced = RainDropTools.GetGForcedScreenMovement(this.camera.transform, this.GForceVector);
        gforced = gforced.normalized;
        dc.transform.localPosition += new Vector3(-gforced.x, -gforced.y, 0f) * 0.01f * Variables.PosYOverLifetime.Evaluate(progress);
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
            if (GetProgress(dc) >= 1.0f)
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
