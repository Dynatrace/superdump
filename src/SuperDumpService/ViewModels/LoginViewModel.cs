using System.ComponentModel.DataAnnotations;

namespace SuperDumpService.ViewModels {
	public class LoginViewModel {
		[DataType(DataType.Url)]
		public string ReturnUrl { get; set; }

		[Required]
		public string Username { get; set; }

		[Required, DataType(DataType.Password)]
		public string Password { get; set; }

		public bool RememberMe { get; set; }

		public bool WrongCredentials { get; set; }
	}
}
