using Arcadia.Repository.EFCore.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Argento.ReportingService.Repository.Model.ModelBase
{
    public class AdminMenuSubLevelsEntity: EntityBase
    {
        public Guid Id { get; set; }
        public string SubLevelName { get; set; }
        public Guid AdminMenuId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class ConfigAdminMenuSubLevelId
    {
        public static Guid SapInternalOrder = new Guid("018fe72f-1d50-77e8-a602-a7cf3e73a3c5"); // SAP Internal Order
        public static Guid FeeAmount = new Guid("018fe72f-6e67-7046-bc1d-95022486c22b"); // Fee Amount
        public static Guid TaxAmount = new Guid("018fe72f-812c-7e63-9dc4-01deecea8924"); // Tax Amount
        public static Guid WHT = new Guid("018fe72f-b065-7f0f-a52a-913cd1037c16"); // WHT
        public static Guid NetAmount = new Guid("018fe730-006e-7c58-bb8e-8a4adb7308fb"); // Net Amount
        public static Guid PaybackDate = new Guid("018fe730-1684-73a9-9b9b-3d968de02097"); // Payback Date
        public static Guid TransferInFrom = new Guid("018fe730-2731-79de-9b1d-e46dbb1ba995"); // Transfer in from
        public static Guid CashReceiveDate = new Guid("018fe730-4143-7698-83e3-bd82751e90c9"); // Cash Receive Date
        public static Guid ReconcileReportNo = new Guid("018fe730-545a-7d19-960e-b3700b22df01"); // Reconcile Report No
        public static Guid Amount = new Guid("018fe730-6733-7334-b1fc-a409da7cdbfe"); // Amount
        public static Guid ComAmount = new Guid("018fe730-7873-76fb-964f-a4a0a70cab50"); // Com Amount
        public static Guid ComVAT = new Guid("018fe730-8b8f-7380-8f4c-a77198a3ba4b"); // Com VAT
        public static Guid NetReceiveFromBank = new Guid("018fe730-9d01-75f2-9752-88ce4fd00935"); // Net Receive from Bank
        public static Guid ComWHT = new Guid("018fe730-ae4d-715d-a5c9-e8565504375b"); // Com WHT
        public static Guid NetAmountWHT = new Guid("018fe730-bda3-7afc-8466-63dccae6f2d6"); // Net Amount WHT
    }
}
