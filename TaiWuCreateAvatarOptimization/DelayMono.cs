using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TaiWuCreateAvatarOptimization
{
    public class DelayMono : MonoBehaviour
    {
        public float DelayTime;
        public Action DelayAction;

        public void Update()
        {
            DelayTime -= Time.deltaTime;
            if (DelayTime < 0)
            {
                try
                {
                    DelayAction();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
                Destroy(gameObject);
            }
        }

        public static void DelayDo(Action action, float time)
        {
            GameObject go = new GameObject($"DelayDo {time}s");
            var d = go.AddComponent<DelayMono>();
            d.DelayAction = action;
            d.DelayTime = time;
        }
    }
}
