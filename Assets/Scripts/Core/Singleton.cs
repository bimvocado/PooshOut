using UnityEngine;

/// <summary>
/// 매니저 클래스들이 공통으로 상속받는 싱글톤 베이스.
/// 이걸 상속하면 매번 싱글톤 코드를 다시 안 짜도 됨.
///
/// 사용법:
///   public class GameManager : Singleton<GameManager> { ... }
///   → 다른 스크립트에서 GameManager.Instance 로 접근
///
/// 주의: 매니저(게임에 하나만 있어야 하는 것)에만 사용.
///       오염물/버블/미생물처럼 여러 개 존재하는 오브젝트엔 쓰지 말 것.
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // 수정
                _instance = FindFirstObjectByType<T>();
            }
            return _instance;
        }
    }

    // 씬이 바뀌어도 매니저가 파괴되지 않게 유지할지 여부.
    // 필요 없으면 자식 클래스에서 false 로 덮어쓰면 됨.
    protected virtual bool Persistent => true;

    protected virtual void Awake()
    {
        // 이미 다른 인스턴스가 있으면 이번 것은 파괴 (중복 방지)
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (Persistent)
        {
            transform.SetParent(null); // DontDestroyOnLoad 는 최상위 오브젝트에만 적용됨
            DontDestroyOnLoad(gameObject);
        }
    }
}
