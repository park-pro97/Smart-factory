//
using System.Collections;

namespace _20240725_ListApp01
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<int> list = new List<int>();
            list.Add(1); list.Add(2); list.Add(3);
            foreach (int i in list) { Console.WriteLine(i); }

            // ArrayList는 <> 사이에 제네릭이 필요가 없음
            ArrayList alist = new ArrayList();
            alist.Add('A'); alist.Add('B'); alist.Add('C');
            alist.Insert(2, 'E'); alist.RemoveAt(0);
            foreach (char ch in alist) { Console.WriteLine(ch); }

            // 각각 명시해주면 정상적으로 출력 가능 - 권장하지 않는 방법
            ArrayList blist = new ArrayList();
            blist.Add(1); blist.Add('Z');
            Console.WriteLine((int)blist[0]); Console.WriteLine((char)blist[1]);
        }
    }
}


------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//


------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//

------------------------------------------------------------------------------------
//
