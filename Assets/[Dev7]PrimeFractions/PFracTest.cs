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
        /*
        PFrac aa = new(false, new() { 0, 2 }, new() { 1, 2 });
        PFrac bb = new(false, new() { 1, 1 }, new() { 2, 0 });

        Debug.Log(aa); //Works implicitly too
        Debug.Log(bb);

        PFrac cc = aa * bb;

        Debug.Log(cc);
        */
        #endregion

        #region MDC and mcm
        /*
        List<byte> aaa = PFrac.NToFactors(24);
        List<byte> bbb = PFrac.NToFactors(36);

        List<byte> ccc = PFrac.FacLCM(aaa, bbb);
        Debug.Log(PFrac.FactorsToN(ccc));
        */
        #endregion

        #region sum and subtraction
        /*
        PFrac az = new(9.5f);
        PFrac bz = new(-0.5f);

        PFrac cz = az + bz;

        Debug.Log(cz);
        */
        #endregion

        #region formula example
        
        float fa = 4.1f;
        float fb = 11;
        float fc = 8.05f;

        float fesult = (fa * fb / 2) + fc;

        PFrac ra = (PFrac)fa;
        PFrac rb = (PFrac)fb;
        PFrac rc = (PFrac)fc;

        PFrac result = (ra * rb / 2) + rc;

        Debug.Log(fesult + " ; " + result);

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
