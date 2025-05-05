using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;


	public class DragPanel : MonoBehaviour,IDragHandler
	{

		void Awake ()
		{

		}


		public void OnDrag (PointerEventData data)
		{
			transform.position = data.position;
		}

	}
