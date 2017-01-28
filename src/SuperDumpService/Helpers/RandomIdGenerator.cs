using System;
using System.Text;

namespace SuperDumpService.Helpers {

	public static class RandomIdGenerator {
		private static char[] chars =
			"abcdefghijklmnopqrstuvwxyz"
			.ToCharArray();
		private static char[] nums =
			"0123456789"
			.ToCharArray();

		private static Random _random = new Random();

		public static string GetRandomId() {
			return GetRandomId(3, 4);
		}

		public static string GetRandomId(int charlength, int numlength) {
			var sb = new StringBuilder(charlength+numlength);

			for (int i = 0; i < charlength; i++) {
				sb.Append(chars[_random.Next(chars.Length)]);
			}
			for (int i = 0; i < numlength; i++) {
				sb.Append(nums[_random.Next(nums.Length)]);
			}
			return sb.ToString();
		}
	}
}
