﻿using System;
using SQLite;
namespace HansWehr
{
	public class RawWordResult
	{
		public int Id { get; set; }
		public string ArabicWord { get; set; }
		public string Definition { get; set; }
		public int RootWordId { get; set; }
		public bool IsRoot { get; set; }
		public byte[] RawMatchInfo { get; set; }
		public string Offsets { get; set; }
	}
}
