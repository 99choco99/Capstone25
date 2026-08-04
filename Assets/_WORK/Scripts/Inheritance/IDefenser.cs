

/// <summary>
/// 방어가 가능한 객체들
/// </summary>
public interface IDefenser
{

    /// <summary>
    /// 피해를 적용하기 전에 방어 가능한지 확인하는 함수
    /// </summary>
    DefenseType DecideDefense(in DamageRequest request);
}

