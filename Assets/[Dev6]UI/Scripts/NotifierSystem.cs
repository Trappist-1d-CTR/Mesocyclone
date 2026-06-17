using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NotifierSystem
{
    public enum Type
    {
        Unknown = 0,
        External = 1,
        Internal = 2,
        Warning = 3,
        Error = 4
    }

    public struct Message
    {
        public string msg;
        public string MET;
        public Type t;
        public float duration;

        public Message(string text, string time, int type)
        {
            msg = text; MET = time; t = (Type)type; duration = 3;
        }
        public Message(string text, string time, int type, float duration)
        {
            msg = text; MET = time; t = (Type)type; this.duration = duration;
        }
    }

    public static List<Message> MainMessageList = new();
    public static List<Message> PiorityMessageList = new();

    public static void Send(string text, string time, int type, float duration)
    {
        Message mem = new(text, time, type, duration);
        MainMessageList.Add(mem);

        if (PiorityMessageList.Count == 0)
        {
            PiorityMessageList.Add(mem);
        }
        else
        {
            int i = 0;
            while (i < PiorityMessageList.Count && (int)PiorityMessageList[i].t >= type)
            {
                i++;
            }
            PiorityMessageList.Insert(i, mem);
        }
        
    }

    public static void Send(string text, string time, int type) => Send(text, time, type, 3);
}
