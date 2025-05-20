using System; // IComparable<T> 등을 사용하기 위해 필요
using System.Collections.Generic;
using UnityEngine; // Unity 관련 코드가 들어갈 수도 있으니 일단 넣어둠

// 제네릭 PriorityQueue 클래스!
// T는 저장할 요소의 타입을 나타내!
// where T : IComparable<T> 는 T 타입이 자기 자신과 비교될 수 있어야 한다는 제약 조건이야!
public class PriorityQueue<T> where T : IComparable<T>
{
    // 힙 데이터를 저장할 리스트 (이제 어떤 타입 T든지 저장할 수 있어!)
    private List<T> heap = new List<T>();

    // 현재 힙에 들어있는 요소 개수
    public int Count
    {
        get { return heap.Count; }
    }

    // 힙이 비어있는지 확인하는 편리한 속성
    public bool IsEmpty
    {
        get { return Count == 0; }
    }


    // 새로운 값을 힙에 추가하는 메서드!
    public void Enqueue(T value)
    {
        // 1. 새 값을 일단 맨 마지막에 추가
        heap.Add(value);

        // 2. Up-Heap (Bubble-Up) 과정 시작!
        int currentIndex = heap.Count - 1; // 새로 추가된 값의 인덱스

        while (currentIndex > 0)
        {
            int parentIndex = (currentIndex - 1) / 2; // 부모 노드의 인덱스

            // *** 여기서 비교! T 타입은 IComparable<T> 제약 조건 때문에 CompareTo 메서드를 가지고 있어! ***
            // CompareTo 결과: 0보다 작으면 (현재 값이 부모보다 작으면), 0이면 같으면, 0보다 크면 크면
            // 최소 힙이므로 현재 값이 부모보다 작으면 자리를 바꿔야 해!
            if (heap[currentIndex].CompareTo(heap[parentIndex]) >= 0) // 현재 값이 부모보다 크거나 같으면 (규칙 만족) 멈춤!
            {
                break;
            }

            // 현재 노드 값과 부모 노드 값 자리 바꾸기!
            Swap(currentIndex, parentIndex);

            // 현재 인덱스를 부모 인덱스로 옮겨서 계속 위로 올라감
            currentIndex = parentIndex;
        }
    }

    // 가장 우선순위가 높은 값 (최소 힙에서는 가장 작은 값)을 꺼내면서 제거하는 메서드!
    public T Dequeue()
    {
        // 힙이 비어있으면 오류 발생!
        if (Count == 0)
        {
            throw new System.InvalidOperationException("PriorityQueue is empty.");
        }

        // 1. 가장 작은 값 (루트 노드 값)을 저장해 둠
        T minValue = heap[0];

        // 2. 힙이 1개 이상의 요소를 가지고 있으면...
        if (Count > 1)
        {
            // 맨 마지막 값을 루트 노드로 옮김
            heap[0] = heap[Count - 1];
            // 맨 마지막 요소 제거
            heap.RemoveAt(Count - 1);

            // 3. Down-Heap (Bubble-Down) 과정 시작!
            DownHeap(0); // 새로운 루트 노드부터 아래로 내려감
        }
        else // 힙에 요소가 1개만 남았다면...
        {
            // 그 하나 남은 요소를 제거
            heap.RemoveAt(0);
        }


        // 4. 가장 작은 값을 반환!
        return minValue;
    }

    // 가장 우선순위가 높은 값 (최소 힙에서는 가장 작은 값)을 확인만 하는 메서드 (제거 안 함)!
    public T Peek()
    {
        // 힙이 비어있으면 오류 발생!
        if (Count == 0)
        {
            throw new System.InvalidOperationException("PriorityQueue is empty.");
        }
        return heap[0]; // 루트 노드 값 반환
    }

    // 힙의 모든 요소를 제거하는 메서드
    public void Clear()
    {
        heap.Clear();
    }


    // 두 인덱스의 요소 자리를 바꾸는 헬퍼 메서드
    private void Swap(int i, int j)
    {
        T temp = heap[i]; // 이제 T 타입 변수 temp!
        heap[i] = heap[j];
        heap[j] = temp;
    }

    // 특정 인덱스부터 아래로 내려가면서 힙 규칙을 맞추는 헬퍼 메서드
    private void DownHeap(int currentIndex)
    {
        while (true)
        {
            int leftChildIndex = 2 * currentIndex + 1; // 왼쪽 자식 인덱스
            int rightChildIndex = 2 * currentIndex + 2; // 오른쪽 자식 인덱스
            int smallestIndex = currentIndex; // 현재 노드, 왼쪽 자식, 오른쪽 자식 중 가장 작은 값의 인덱스

            // 왼쪽 자식이 있고, 왼쪽 자식 값이 (현재까지 가장 작은 값인) smallestIndex 위치의 값보다 작으면
            // *** 여기서 비교! T 타입을 CompareTo로 비교! ***
            if (leftChildIndex < Count && heap[leftChildIndex].CompareTo(heap[smallestIndex]) < 0)
            {
                smallestIndex = leftChildIndex; // 가장 작은 값은 왼쪽 자식!
            }

            // 오른쪽 자식이 있고, 오른쪽 자식 값이 (현재까지 가장 작은 값인) smallestIndex 위치의 값보다 작으면
            // *** 여기서 비교! T 타입을 CompareTo로 비교! ***
            if (rightChildIndex < Count && heap[rightChildIndex].CompareTo(heap[smallestIndex]) < 0)
            {
                smallestIndex = rightChildIndex; // 가장 작은 값은 오른쪽 자식!
            }

            // 현재 노드 값이 자식들보다 작거나 같으면 (힙 규칙 만족) 멈춤!
            if (smallestIndex == currentIndex)
            {
                break;
            }

            // 가장 작은 값을 가진 자식 노드와 현재 노드 자리 바꾸기!
            Swap(currentIndex, smallestIndex);

            // 현재 인덱스를 가장 작은 값을 가진 자식 인덱스로 옮겨서 계속 아래로 내려감
            currentIndex = smallestIndex;
        }
    }
}
