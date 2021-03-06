﻿using System;
using System.Collections.Generic;

namespace HansWehr
{
	public class MatchInfo
	{

		private static int _phraseCountPosition = 0;
		private static int _columnCountPosition = 4;
		private static int _rowCountPosition = 8;
		private static int _averageTokenCountsPosition = 12;
		private int TokenCountsPosition { get { return _averageTokenCountsPosition + (ColumnCount * 4); } }
		private int PhraseDatasPosition { get { return TokenCountsPosition + (ColumnCount * 4); } }

		/// <summary>
		/// Gets or sets the number of phrases in the query.
		/// </summary>
		/// <value>The number of phrases in the query.</value>
		public int PhraseCount { get; set; }

		/// <summary>
		/// Gets or sets the number of columns in the fts table this result belongs to.
		/// </summary>
		/// <value>The number of columns in the fts table this result belongs to.</value>
		public int ColumnCount { get; set; }

		/// <summary>
		/// Gets or sets the number of rows returned by the query.
		/// </summary>
		/// <value>The number of rows returned by the query.</value>
		public int RowCount { get; set; }

		/// <summary>
		/// Gets or sets the average token count of each column in the FTS table
		/// </summary>
		/// <value>The average token count of each column in the FTS table.</value>
		public int[] AverageTokenCounts { get; set; }

		/// <summary>
		/// Gets or sets the token count for each column for the current row in the FTS table.
		/// </summary>
		/// <value>The token count for each column for the current row in the FTS table.</value>
		public int[] TokenCounts { get; set; }

		/// <summary>
		/// Gets or sets the details on occurances of each phrase in respect to this row.
		/// </summary>
		/// <value>Details on occurances of each phrase in respect to this row.</value>
		public PhraseData[] PhraseDatas { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:HansWehr.MatchInfo"/> class.
		/// </summary>
		/// <param name="rawBytes">The raw blob from the sqlite function matchInfo(tableName,'pcnalx') as a byte array</param>
		public MatchInfo(IEnumerable<string> phrases, byte[] rawBytes)
		{
			// there should be a minimum of 32 bytes in a valid matchinfo('pcnalx')
			if (rawBytes == null || rawBytes.Length < 32) throw new ArgumentException("The byte array length is incorrect");

			try
			{


				// initialise the single properties with their values
				PhraseCount = BitConverter.ToInt32(rawBytes, _phraseCountPosition);
				ColumnCount = BitConverter.ToInt32(rawBytes, _columnCountPosition);
				RowCount = BitConverter.ToInt32(rawBytes, _rowCountPosition);

				// initialise the array properties as empty arrays
				AverageTokenCounts = new int[ColumnCount];
				TokenCounts = new int[ColumnCount];
				PhraseDatas = new PhraseData[PhraseCount];

				// everything before averageTokenCounts has already been extracted above
				// so we go from the beginning of average token counts to the end of the byte array to extract everything else
				for (int i = _averageTokenCountsPosition; i < rawBytes.Length; i += 4)
				{
					int currentInt = BitConverter.ToInt32(rawBytes, i);

					if (i < TokenCountsPosition)
					{
						// still looking at average token counts, add to the property
						int row = (i - _averageTokenCountsPosition) / 4;
						AverageTokenCounts[row] = currentInt;
					}
					else if (i < PhraseDatasPosition)
					{
						// now looking at token counts, add to the property
						int row = (i - TokenCountsPosition) / 4;
						TokenCounts[row] = currentInt;
					}
					else
					{
						// now we're looking at phrase datas, which are 3 values for each phrase for each colomn (3 * n(col) * n(phrase))

						// work out or current position relative to the start of phrase datas
						int position = (i - PhraseDatasPosition) / 4;

						// floor the index by the number of bytes for each phrase to get the phrase index
						// so if there are 3 phrases and 2 columns, each phrase will have (3*2) values,
						// 0 - 5 will give us 0, 6-11 will give us 1, 12-17 gives us 2, which is the correct phrase index 
						int phraseIndex = position / (ColumnCount * 3);

						// this tells us which column we are looking at within the phrase
						// same example as above, our position will be between 0 and 17,
						// all even numbers will be for the first column of the respective phrase
						// all odd numbers will be for the second column of the respective phrase
						int columnIndex = (position / 3) % ColumnCount;


						if (PhraseDatas[phraseIndex] == null)
							PhraseDatas[phraseIndex] = new PhraseData { ColumnDatas = new PhraseColumnData[ColumnCount] };

						PhraseDatas[phraseIndex].ColumnDatas[columnIndex] = new PhraseColumnData
						{
							CurrentRowTermFrequency = currentInt,
							TotalTermFrequency = BitConverter.ToInt32(rawBytes, i + 4),
							MatchCount = BitConverter.ToInt32(rawBytes, i + 8)
						};

						// skip the next to bytes as we extracted them above
						i += 8;
					}
				}
			}
			catch (IndexOutOfRangeException e)
			{
				throw new ArgumentException("The byte array length is incorrect", e);
			}


		}
	}

	public class PhraseData
	{
		/// <summary>
		/// Gets or sets the column datas.
		/// </summary>
		/// <value>Details on the phrase occurances for each column.</value>
		public PhraseColumnData[] ColumnDatas { get; set; }
	}

	public class PhraseColumnData
	{
		/// <summary>
		/// Gets or sets the number of times the phrase appears in the column for the current row.
		/// </summary>
		/// <value>The number of times the phrase appears in the column for the current row.</value>
		public int CurrentRowTermFrequency { get; set; }

		/// <summary>
		/// Gets or sets the total number of times the phrase appears in the column in all rows in the FTS table.
		/// </summary>
		/// <value>The total number of times the phrase appears in the column in all rows in the FTS table.</value>
		public int TotalTermFrequency { get; set; }

		/// <summary>
		/// Gets or sets the total number of rows in the FTS table for which the column contains at least one instance of the phrase.
		/// </summary>
		/// <value>The total number of rows in the FTS table for which the column contains at least one instance of the phrase.</value>
		public int MatchCount { get; set; }
	}
}
