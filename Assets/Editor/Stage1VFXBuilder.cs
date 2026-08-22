using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Stage1 VFX 3종(물보라/속도선/벽 스파크) 프리팹을 코드로 생성하는 에디터 툴.
/// 메뉴: Tools > Stage1 VFX > Build All Prefabs
/// 배치모드에서 -executeMethod Stage1VFXBuilder.BuildAll 로도 실행 가능.
/// URP 프로젝트라 소스 에셋(Farming_game_FX 등)의 Built-in RP 파티클 셰이더에 기대지 않고,
/// URP Particles/Unlit 셰이더 + Unity 기본 제공 소프트 도트 텍스처로 새로 제작한다.
/// </summary>
public static class Stage1VFXBuilder
{
    private const string RootFolder = "Assets/Art/VFX/Stage1";
    private const string MatFolder = "Assets/Art/VFX/Stage1/Materials";

    [MenuItem("Tools/Stage1 VFX/Build All Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(MatFolder);

        Texture2D dot = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");

        Material dropletMat = CreateParticleMaterial(
            MatFolder + "/Mat_ST1_WaterSplash_Droplet.mat",
            new Color(0.45f, 0.8f, 1f, 0.9f), dot, additive: false);
        Material foamMat = CreateParticleMaterial(
            MatFolder + "/Mat_ST1_WaterSplash_Foam.mat",
            new Color(0.85f, 0.96f, 1f, 0.55f), dot, additive: false);
        Material speedLineMat = CreateParticleMaterial(
            MatFolder + "/Mat_ST1_SpeedLine.mat",
            new Color(0.75f, 0.95f, 1f, 1f), dot, additive: true);
        Material sparkMat = CreateParticleMaterial(
            MatFolder + "/Mat_ST1_WallSpark.mat",
            new Color(1f, 0.8f, 0.35f, 1f), dot, additive: true);

        BuildWaterSplash(dropletMat, foamMat);
        BuildSpeedLine(speedLineMat);
        BuildWallSpark(sparkMat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Stage1VFXBuilder] Stage1 VFX 프리팹 3종 생성 완료 (" + RootFolder + ")");
    }

    // ==================================================
    // 1. 물보라 (Water Splash)
    // ==================================================
    private static void BuildWaterSplash(Material dropletMat, Material foamMat)
    {
        var root = new GameObject("VFX_ST1_WaterSplash");
        root.AddComponent<WaterSplashVFX>();

        // 물방울: 위로 튀는 작은 파란/청록 입자
        var droplets = CreateChildParticleSystem(root.transform, "Droplets", new Vector3(-90f, 0f, 0f));
        var dMain = droplets.main;
        dMain.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
        dMain.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        dMain.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        dMain.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        dMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        dMain.loop = true;
        dMain.playOnAwake = false;
        dMain.maxParticles = 80;
        dMain.gravityModifier = 1.1f;

        var dEmission = droplets.emission;
        dEmission.rateOverTime = 18f;

        var dShape = droplets.shape;
        dShape.shapeType = ParticleSystemShapeType.Cone;
        dShape.angle = 22f;
        dShape.radius = 0.12f;

        SetFadeColorOverLifetime(droplets, 0.9f);
        SetShrinkSizeOverLifetime(droplets, 1f, 0.5f);
        SetRenderer(droplets, ParticleSystemRenderMode.Billboard, dropletMat, 0f, 0f);

        // 폼: 크고 느리게 퍼지는 흰 거품
        var foam = CreateChildParticleSystem(root.transform, "Foam", new Vector3(-90f, 0f, 0f));
        var fMain = foam.main;
        fMain.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        fMain.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
        fMain.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        fMain.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        fMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        fMain.loop = true;
        fMain.playOnAwake = false;
        fMain.maxParticles = 40;
        fMain.gravityModifier = 0.4f;

        var fEmission = foam.emission;
        fEmission.rateOverTime = 8f;

        var fShape = foam.shape;
        fShape.shapeType = ParticleSystemShapeType.Cone;
        fShape.angle = 30f;
        fShape.radius = 0.15f;

        SetFadeColorOverLifetime(foam, 0.6f);
        SetShrinkSizeOverLifetime(foam, 1f, 0.6f);
        SetRenderer(foam, ParticleSystemRenderMode.Billboard, foamMat, 0f, 0f);

        SaveAsPrefabAndDestroy(root, RootFolder + "/VFX_ST1_WaterSplash.prefab");
    }

    // ==================================================
    // 2. 속도선 (Speed Line) - 버블 획득 시, 월드스페이스로 시야 주변에서 안쪽으로 수렴
    // ==================================================
    private static void BuildSpeedLine(Material mat)
    {
        var root = new GameObject("VFX_ST1_SpeedLine");
        root.AddComponent<TimedVFX>(); // 기본 lifetime(1.5s) 후 자동 파괴

        var ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = 0f; // 속도는 velocityOverLifetime의 radial로만 제어
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // 앵커(카메라)를 따라 움직이도록 로컬 기준
        main.maxParticles = 100;
        main.stopAction = ParticleSystemStopAction.None; // TimedVFX가 정리

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 55) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.6f;
        shape.radiusThickness = 0f; // 구 표면(shell)에서만 발생 → 시야 주변 링 형태

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(-9f); // 음수 = 중심(플레이어)으로 수렴

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        SetRenderer(ps, ParticleSystemRenderMode.Stretch, mat, lengthScale: 4f, velocityScale: 0.6f);

        SaveAsPrefabAndDestroy(root, RootFolder + "/VFX_ST1_SpeedLine.prefab");
    }

    // ==================================================
    // 3. 벽 충돌 스파크 (Wall Hit Spark)
    // ==================================================
    private static void BuildWallSpark(Material mat)
    {
        var root = new GameObject("VFX_ST1_WallSpark");
        root.AddComponent<TimedVFX>();

        var ps = root.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.6f, 1f), new Color(1f, 0.6f, 0.15f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.gravityModifier = 1.3f;
        main.stopAction = ParticleSystemStopAction.None; // TimedVFX가 정리

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

        // 로컬 +Z(정면) 방향으로 원뿔형 발산 → 스폰 시 Quaternion.LookRotation(normal)로 벽 바깥쪽을 향하게 회전시켜서 사용
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.03f;

        SetFadeColorOverLifetime(ps, 1f);
        SetShrinkSizeOverLifetime(ps, 1f, 0.2f);
        SetRenderer(ps, ParticleSystemRenderMode.Stretch, mat, lengthScale: 3f, velocityScale: 0.3f);

        SaveAsPrefabAndDestroy(root, RootFolder + "/VFX_ST1_WallSpark.prefab");
    }

    // ==================================================
    // 공용 헬퍼
    // ==================================================

    private static ParticleSystem CreateChildParticleSystem(Transform parent, string name, Vector3 localEuler)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localRotation = Quaternion.Euler(localEuler);
        return go.AddComponent<ParticleSystem>();
    }

    private static void SetFadeColorOverLifetime(ParticleSystem ps, float startAlpha)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(startAlpha, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    private static void SetShrinkSizeOverLifetime(ParticleSystem ps, float from, float to)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, from, 1f, to));
    }

    private static void SetRenderer(ParticleSystem ps, ParticleSystemRenderMode mode, Material mat, float lengthScale, float velocityScale)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = mode;
        renderer.material = mat;
        if (mode == ParticleSystemRenderMode.Stretch)
        {
            renderer.lengthScale = lengthScale;
            renderer.velocityScale = velocityScale;
        }
        renderer.alignment = ParticleSystemRenderSpace.View;
    }

    private static Material CreateParticleMaterial(string path, Color tint, Texture2D dotTexture, bool additive)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("[Stage1VFXBuilder] URP Particles/Unlit 셰이더를 찾지 못해 기본 Sprites/Default로 대체합니다. URP 패키지 설치 상태를 확인하세요.");
            shader = Shader.Find("Sprites/Default");
        }

        var mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", dotTexture);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", dotTexture);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", additive ? 2f : 0f); // 0 Alpha, 2 Additive (URP BaseShaderGUI 순서)
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);

        mat.SetInt("_SrcBlend", (int)(additive ? BlendMode.One : BlendMode.SrcAlpha));
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        if (!additive) mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (additive) mat.EnableKeyword("_BLENDMODE_ADD");

        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void SaveAsPrefabAndDestroy(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
