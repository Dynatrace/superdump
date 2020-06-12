using System.Threading.Tasks;
using SuperDumpService.Models;

namespace SuperDumpService.Services {
	public interface IFaultReportSender {
		Task SendFaultReport(DumpMetainfo dumpInfo, FaultReport faultReport);
	}
}