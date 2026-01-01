using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PFracTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        #region Initial Test
        /*
        PFrac a = PFrac.zero;
        PFrac b = PFrac.minus1;
        PFrac c = PFrac.plus1;

        Debug.Log("Test: " + a.ToString() + " ; " + b.ToString() + " ; " + c.ToString());

        PFrac d = new(Mathf.Pow(10, 10));

        Debug.Log(d.ToString());
        */
        #endregion

        #region multiplication

        PFrac aa = new(false, new() { 0, 2 }, new() { 1, 2 });
        PFrac bb = new(false, new() { 1, 1 }, new() { 2, 0 });

        Debug.Log(aa); //Works implicitly too
        Debug.Log(bb);

        PFrac cc = aa * bb;

        Debug.Log(cc);

        #endregion

        /*float eps = 1.0f;
        float toot = 2.0f;
        int i = 0;
        while (toot > 1.0f && i < 9001)
        {
            eps /= 2;
            toot = 1.0f + eps;
            i++;
        }
        eps *= 2; i--;
        Debug.Log(eps + " ; 2^(-" + i + ")");

        toot = 1.0f + eps;
        Debug.Log(toot > 1.0f);*/

        /*
        float y = 1.0f + Mathf.Pow(2, -23);
        float z = 1.0f;
        Debug.Log(y > z);*/
    }
}
