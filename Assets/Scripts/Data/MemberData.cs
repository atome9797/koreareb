using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Data;

[Serializable]
public class MemberData
{
    public int mem_idx;
    public string mem_auth_id;
    public int mem_status = 0;
    public string mem_user_id;
    public string mem_name;
    public string mem_password;
    public string mem_depart;
    public string mem_title;
    public string mem_work;
    public string create_date;
    public string change_date;
    public int mem_failcount;

    public MemberData(DataRow row)
    {
        this.mem_idx = int.Parse(row["mem_idx"].ToString());

        this.mem_auth_id = row["mem_auth_id"].ToString();
        this.mem_status = int.Parse(row["mem_status"].ToString());
        this.mem_user_id = row["mem_userid"].ToString();
        this.mem_name = row["mem_name"].ToString();
        this.mem_password = row["mem_password"].ToString();
        this.mem_depart = row["mem_depart"].ToString();
        this.mem_title = row["mem_title"].ToString();
        this.mem_work = row["mem_work"].ToString();
        this.create_date = row["create_date"].ToString();
        this.change_date = row["change_date"].ToString();
        this.mem_failcount = int.Parse(row["mem_failcount"].ToString());
    }

    public MemberData(int mem_idx, string mem_auth_id, int mem_status, string mem_user_id, string mem_name, string mem_password, string mem_depart, string mem_title, string mem_work, string create_date, string change_date, int mem_failcount)
    {
        this.mem_idx = mem_idx;
        this.mem_auth_id = mem_auth_id;
        this.mem_status = mem_status;
        this.mem_user_id = mem_user_id;
        this.mem_name = mem_name;
        this.mem_password = mem_password;
        this.mem_depart = mem_depart;
        this.mem_title = mem_title;
        this.mem_work = mem_work;
        this.create_date = create_date;
        this.change_date = change_date;
        this.mem_failcount = mem_failcount;
    }

    public override string ToString()
    {
        return $"MemberData : {mem_idx}, {mem_auth_id}, {mem_status}, {mem_user_id}, {mem_name}," +
            $" {mem_password}, {mem_depart}, {mem_title}, {mem_work}, {create_date}, {change_date}, {mem_failcount}";
    }

}
