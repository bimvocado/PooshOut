using System.Collections.Generic;
using UnityEngine;

/// [poo]_Water 조각들을 컨베이어 벨트처럼 순환시킨다.
/// 전부 같은 방향(-Z, 플레이어 쪽)으로 흐르다가, 화면 밖으로 완전히 나간 조각을
/// 전체 길이만큼 뒤로 순간이동시켜서 다시 맨 뒤에 붙인다.
/// 조각 간격이 균일해야 이음매 없이 자연스럽게 이어진다.
public class WaterConveyor : MonoBehaviour
{
    [Tooltip("흐르는 물 조각들. 순서는 상관없음 - 간격만 균일하면 됨.")]
    [SerializeField] private List<Transform> waterPieces = new List<Transform>();

    [Tooltip("조각 사이의 Z축 간격(m). 지금 세팅 기준 7.5.")]
    [SerializeField] private float pieceSpacing = 7.5f;

    [Tooltip("흐르는 속도(m/s). 게이트 접근 속도(1.5)와 비슷하게 맞추면 자연스러움.")]
    [SerializeField] private float flowSpeed = 1.5f;

    [Tooltip("이 Z값보다 작아지면(플레이어를 완전히 지나치면) 순간이동으로 맨 뒤에 재배치. " +
             "카메라 시야보다 확실히 뒤쪽으로 넉넉하게 잡을 것.")]
    [SerializeField] private float resetThresholdZ = -5f;

    private float _totalLength;

    private void Start()
    {
        _totalLength = pieceSpacing * waterPieces.Count;
    }

    private void Update()
    {
        float delta = flowSpeed * Time.deltaTime;

        foreach (Transform piece in waterPieces)
        {
            if (piece == null) continue;

            Vector3 pos = piece.position;
            pos.z -= delta;

            // 플레이어를 완전히 지나쳐 화면 밖으로 나가면, 전체 길이만큼 뒤로 보내
            // 대열 맨 끝에 다시 붙는 것처럼 만든다. 간격이 균일하므로 이음매가 안 보인다.
            if (pos.z < resetThresholdZ)
            {
                pos.z += _totalLength;
            }

            piece.position = pos;
        }
    }
}
