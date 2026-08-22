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
///
/// 재실행 안전성: 이 메뉴는 여러 번 실행돼도(재빌드) 항상 같은 경로의 에셋을 "덮어쓰기"해야 한다.
/// AssetDatabase.CreateAsset은 그 경로에 이미 에셋이 있으면 새로 만들지 않고 조용히 실패하는데,
/// 그 상태로 프리팹을 저장하면 프리팹이 "저장되지 않은 임시 머티리얼"을 참조하게 되어
/// 런타임에 마젠타(핑크, 머티리얼 유실)로 깨진다. 그래서 머티리얼/텍스처는 만들기 전에
/// 기존 에셋을 먼저 지우고(DeleteAssetIfExists) 다시 만든다.
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
        Material sparkMat = CreateParticleMaterial(
            MatFolder + "/Mat_ST1_WallSpark.mat",
            new Color(1f, 0.8f, 0.35f, 1f), dot, additive: true);

        BuildWaterSplash(dropletMat, foamMat);
        BuildSpeedLineQuad();
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
    // 2. 속도선 (Speed Line) - 만화책 집중선(speed lines) 스타일.
    //    파티클로는 "화면 가장자리에만, 중앙은 완전히 비움"을 물리적으로 보장하기 어려워서
    //    (파티클 확산 범위를 아무리 좁혀도 뷰 전체에 거미줄처럼 퍼져 보이는 문제가 있었음),
    //    대신 중앙이 뚫린 방사형 선 패턴을 알파 텍스처로 직접 그려서 카메라 앞에 고정한 쿼드에 입힌다.
    //    쿼드 자체가 카메라(앵커)의 자식이라 VR에서 고개를 돌려도 항상 시야에 딱 붙어 보인다.
    //    한 번만 생성해 재사용하고(TimedVFX로 매번 파괴/재생성하지 않음) SpeedLineFlash가 알파를
    //    0→peak→0으로 페이드해서 "잠깐 나타났다 사라짐"을 구현한다.
    // ==================================================
    private static void BuildSpeedLineQuad()
    {
        Texture2D rawTex = CreateSpeedLineTexture();
        string texPath = RootFolder + "/Tex_ST1_SpeedLines.png";
        SaveTextureAsAsset(rawTex, texPath);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        Material mat = CreateUnlitQuadMaterial(MatFolder + "/Mat_ST1_SpeedLine.mat", tex);

        var root = GameObject.CreatePrimitive(PrimitiveType.Quad);
        root.name = "VFX_ST1_SpeedLine";
        Object.DestroyImmediate(root.GetComponent<Collider>()); // 순수 비주얼용 - 충돌 판정 불필요

        root.transform.localPosition = new Vector3(0f, 0f, 0.6f); // 카메라 앞 0.6m 고정
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = new Vector3(1.8f, 1.8f, 1f);  // 대략적인 VR FOV를 덮는 크기 (필요시 인스펙터에서 조정)

        var mr = root.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        root.AddComponent<SpeedLineFlash>();

        SaveAsPrefabAndDestroy(root, RootFolder + "/VFX_ST1_SpeedLine.prefab");
    }

    /// <summary>
    /// 중심은 완전히 비우고 화면 가장자리 부근에만 방사형 흰 선이 뻗어나가는 알파 텍스처를 절차적으로 생성.
    /// 항상 같은 시드를 써서 다시 빌드해도 매번 같은 패턴이 나온다(재생성 시 결과가 흔들리지 않도록).
    /// </summary>
    private static Texture2D CreateSpeedLineTexture(int size = 512, int streakCount = 40)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 0);

        var rng = new System.Random(1234);
        float center = size * 0.5f;
        float maxRadius = size * 0.5f; // 텍스처에 내접하는 원까지만 그림 (화면 가장자리 정도에서 끝나도록)

        for (int s = 0; s < streakCount; s++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2);
            float innerT = 0.45f + (float)rng.NextDouble() * 0.15f; // 이 비율 안쪽은 완전히 비움(중앙 공백)
            float outerT = 0.85f + (float)rng.NextDouble() * 0.15f; // 가장자리 살짝 넘어서까지
            float halfWidthRad = Mathf.Deg2Rad * (0.4f + (float)rng.NextDouble() * 1.2f);
            float dirX = Mathf.Cos(angle), dirY = Mathf.Sin(angle);
            float perpX = -dirY, perpY = dirX;

            int minR = Mathf.FloorToInt(innerT * maxRadius);
            int maxR = Mathf.Min(size - 1, Mathf.CeilToInt(outerT * maxRadius));

            for (int r = minR; r <= maxR; r++)
            {
                float t = Mathf.InverseLerp(innerT * maxRadius, outerT * maxRadius, r);
                float lengthFade = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI); // 시작/끝에서 부드럽게 사라짐

                int thickness = Mathf.Max(1, Mathf.RoundToInt(r * halfWidthRad));
                for (int w = -thickness; w <= thickness; w++)
                {
                    int px = Mathf.RoundToInt(center + dirX * r + perpX * w);
                    int py = Mathf.RoundToInt(center + dirY * r + perpY * w);
                    if (px < 0 || px >= size || py < 0 || py >= size) continue;

                    float widthFade = 1f - Mathf.Abs(w) / (float)(thickness + 1);
                    byte alpha = (byte)Mathf.Clamp(lengthFade * widthFade * 255f, 0, 255);
                    int idx = py * size + px;
                    pixels[idx].a = (byte)Mathf.Max(pixels[idx].a, alpha);
                }
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    /// <summary>절차 생성한 텍스처를 PNG 파일로 저장하고 알파-투명 스프라이트로 임포트 설정을 맞춘다.</summary>
    private static void SaveTextureAsAsset(Texture2D tex, string path)
    {
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); // 메모리상 임시 텍스처 - 디스크에 쓴 뒤엔 필요 없음
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }

    /// <summary>쿼드 메시용 알파 블렌드 URP Unlit 머티리얼. 평소엔 알파 0(완전 투명) - SpeedLineFlash가 재생 시에만 올린다.</summary>
    private static Material CreateUnlitQuadMaterial(string path, Texture2D tex)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            Debug.LogWarning("[Stage1VFXBuilder] URP Unlit 셰이더를 찾지 못해 기본 Sprites/Default로 대체합니다. URP 패키지 설치 상태를 확인하세요.");
            shader = Shader.Find("Sprites/Default");
        }

        var mat = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 1f, 1f, 0f));

        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f);     // Alpha
        if (mat.HasProperty("_ZWrite")) mat.SetFloat("_ZWrite", 0f);
        if (mat.HasProperty("_Cull")) mat.SetFloat("_Cull", (float)CullMode.Off);
        if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);

        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.SetOverrideTag("RenderType", "Transparent");

        DeleteAssetIfExists(path);
        AssetDatabase.CreateAsset(mat, path);
        VerifyShaderApplied(mat, shader);
        return mat;
    }

    /// <summary>
    /// 의도한 셰이더가 실제로 머티리얼에 붙었는지, 그리고 그 셰이더가 현재 플랫폼에서 지원되는지
    /// 콘솔에 명시적으로 남긴다. 여기서 아무 경고도 안 뜨는데 에디터에서 마젠타로 보인다면
    /// 머티리얼 에셋 자체의 문제가 아니라 Library 캐시가 꼬였거나(에디터 재시작/Reimport All 필요),
    /// 빌드에서만 재현된다면 셰이더 스트리핑 문제(Always Included Shaders 확인) 쪽을 봐야 한다.
    /// </summary>
    private static void VerifyShaderApplied(Material mat, Shader expectedShader)
    {
        if (mat.shader != expectedShader)
        {
            Debug.LogError($"[Stage1VFXBuilder] '{mat.name}'에 의도한 셰이더가 적용되지 않았습니다. " +
                $"기대: {expectedShader?.name ?? "null"} / 실제: {mat.shader?.name ?? "null"}");
        }
        else if (!mat.shader.isSupported)
        {
            Debug.LogError($"[Stage1VFXBuilder] '{mat.name}'의 셰이더 '{mat.shader.name}'가 " +
                "현재 플랫폼에서 지원되지 않습니다(isSupported=false) - 이 경우 에디터에서도 마젠타로 보입니다.");
        }
        else
        {
            Debug.Log($"[Stage1VFXBuilder] '{mat.name}' → 셰이더 '{mat.shader.name}' 정상 적용 확인.");
        }
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

        // 에디터에서 생성 도중 재생되다 만 상태가 프리팹에 그대로 저장되지 않도록 저장 전 확실히 정지/초기화.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

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
        // renderer.material(비공유 setter)은 에디터에서 즉석으로 만든 자식 오브젝트에 쓰면
        // 머티리얼을 인스턴스화하려다 프리팹 저장 시 참조를 제대로 못 들고 가서 None(마젠타)으로
        // 저장되는 경우가 있었다(WaterSplash의 Droplets/Foam 자식에서 실제로 재현됨 - 루트에 바로
        // 붙는 WallSpark는 우연히 괜찮았음). 에디터 스크립트에서 에셋을 연결할 땐 항상 sharedMaterial을
        // 써야 한다.
        renderer.sharedMaterial = mat;
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

        DeleteAssetIfExists(path);
        AssetDatabase.CreateAsset(mat, path);
        VerifyShaderApplied(mat, shader);
        return mat;
    }

    private static void SaveAsPrefabAndDestroy(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// 재실행해도 안전하게 덮어써지도록, 만들기 전에 같은 경로의 기존 에셋을 지운다.
    /// AssetDatabase.CreateAsset은 경로가 이미 차 있으면 새로 만들지 않고 조용히 실패하므로,
    /// 그걸 모르고 지나가면 방금 만든(디스크에 저장 안 된) 임시 머티리얼을 프리팹이 참조하게 되어
    /// 런타임에 마젠타로 깨진다.
    /// </summary>
    private static void DeleteAssetIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
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
