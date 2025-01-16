using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Collections;

namespace LAB2D
{
	public class AsyncProgressUI : MonoBehaviour
	{
		public static AsyncProgressUI Instance { private set; get; }
		public long CurProcess { get; set; }
		public delegate void Complete();
		public Complete complete;
		
		private int total;
		private Text tip; // 提示信息
		private Text percent; // 百分比
		private Slider slider; // 进度条
		private bool isOne = false; // 进度条

		private void Awake()
        {
			Instance = this;
			tip = transform.Find("Center/Tips").GetComponent<Text>();
			if (tip == null)
			{
				Debug.LogError("tips Not Found!!!");
				return;
			}
			percent = transform.Find("Center/Percent").GetComponent<Text>();
			if (percent == null)
			{
				Debug.LogError("percent Not Found!!!");
				return;
			}
			slider = transform.Find("Center/ProgressBar").GetComponent<Slider>();
			if (slider == null)
			{
				Debug.LogError("slider Not Found!!!");
				return;
			}
		}

		public void addOneProcess()
		{
			CurProcess += 1;
			show();
			if(CurProcess >= total && !isOne)
			{
				isOne = true;
				StartCoroutine(_complete());
			}
		}

		public IEnumerator _complete()
		{
			yield return new WaitForSeconds(0.5f);
			complete();
		}

		public void setTip(string tip)
		{
			this.tip.text = tip;
			show();
		}

		public void addTotal(int total) {
            if (total < 0)
            {
				Debug.LogError("不能为负值");
            }
			this.total += total;
		}

		private void show()
		{
			percent.text = "当前进度:" + (CurProcess * 1000 / total / 10.0f).ToString() + "%";
			slider.value = CurProcess * 1.0f / total;
		}
	}
}