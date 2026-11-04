using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DevExtreme.AspNet.Data;
using Web.Controllers.Api.Workflow;
using Web.Data.DbContexts;
using Web.Data.Entities.Workflow;
using Web.Data.Enums.Workflow;
using Web.Models.Dtos.Workflow;
using Web.Services.Workflow;

// Verify multipart numeric binding with the same Vietnamese culture used by the application.
var previousCulture=System.Globalization.CultureInfo.CurrentCulture;
System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
try {
    var http=new DefaultHttpContext();http.Request.ContentType="application/x-www-form-urlencoded";
    http.Request.Form=new FormCollection(new Dictionary<string,Microsoft.Extensions.Primitives.StringValues>{{"BusinessData.ExchangeRate","180.25"}});
    var action=new ActionContext(http,new Microsoft.AspNetCore.Routing.RouteData(),new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    var factories=new List<Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory>{new Microsoft.AspNetCore.Mvc.ModelBinding.FormValueProviderFactory()};
    var resource=new Microsoft.AspNetCore.Mvc.Filters.ResourceExecutingContext(action,new List<Microsoft.AspNetCore.Mvc.Filters.IFilterMetadata>(),factories);
    new Web.Common.InvariantFormValuesAttribute().OnResourceExecuting(resource);
    var providerContext=new Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext(action);
    foreach(var factory in factories)await factory.CreateValueProviderAsync(providerContext);
    var metadata=new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider().GetMetadataForType(typeof(decimal));
    var binding=Microsoft.AspNetCore.Mvc.ModelBinding.DefaultModelBindingContext.CreateBindingContext(action,new Microsoft.AspNetCore.Mvc.ModelBinding.CompositeValueProvider(providerContext.ValueProviders),metadata,null,"BusinessData.ExchangeRate");
    await new Microsoft.AspNetCore.Mvc.ModelBinding.Binders.SimpleTypeModelBinder(typeof(decimal),Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).BindModelAsync(binding);
    if(!binding.Result.IsModelSet||(decimal)binding.Result.Model!=180.25m)throw new Exception("Vietnamese UI must preserve decimal input values.");
    var decimalLine=new RequestLineInputDto{Quantity=.5m,UnitPrice=1.25m}; if(!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(decimalLine,new System.ComponentModel.DataAnnotations.ValidationContext(decimalLine),new List<System.ComponentModel.DataAnnotations.ValidationResult>(),true))throw new Exception("Decimal quantity validation must be culture-invariant."); Console.WriteLine("PASS: Vietnamese culture form binding and decimal range validation.");
} finally {System.Globalization.CultureInfo.CurrentCulture=previousCulture;}
var sourceRoot=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../"));
if(!File.Exists(Path.Combine(sourceRoot,"Web","appsettings.json"))) sourceRoot=Directory.GetCurrentDirectory();
PdfVerification.Run(sourceRoot);
var config=new ConfigurationBuilder().SetBasePath(Path.Combine(sourceRoot,"Web")).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json",optional:true).AddEnvironmentVariables().Build();
var connection=new SqlConnectionStringBuilder(config.GetConnectionString("DefaultConnection"));
var server=connection.DataSource.ToLowerInvariant();
var serverHost=server.Replace("tcp:", "").Split(new[]{(char)92,(char)44})[0]; if(!new[]{"localhost","127.0.0.1",".","(local)","(localdb)",Environment.MachineName.ToLowerInvariant()}.Contains(serverHost))throw new InvalidOperationException("Verification only creates disposable databases on a local SQL Server.");
var testName="Codex_RequestForms_Test_"+Guid.NewGuid().ToString("N");
connection.InitialCatalog=testName;connection.ConnectTimeout=10;
var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString,o=>o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)).Options;
await using var db=new ApplicationDbContext(options);
var created=false;
void Check(bool condition,string message){if(!condition)throw new Exception(message);}
async Task Invalid(Func<Task> operation,string message){try{await operation();}catch(InvalidOperationException){return;}throw new Exception(message);}
try {
    created=true;await db.Database.MigrateAsync();
    Check(await db.RequestTypes.CountAsync()==8,"Migration must provide all eight templates.");
    var employee=await db.AppUsers.SingleAsync(x=>x.UserName=="employee1");var manager=await db.AppUsers.SingleAsync(x=>x.UserName=="manager1");
    var partner=new RequestMasterItem{Category="Partner",Code="TEST_PARTNER",Name="Đối tác kiểm thử"};
    var vehicle=new RequestMasterItem{Category="Vehicle",Code="TEST_VEHICLE",Name="Xe kiểm thử",ParentId=111,Capacity=6};
    var bank=new RequestMasterItem{Category="Bank",Code="TEST_BANK",Name="Ngân hàng kiểm thử"}; db.AddRange(partner,vehicle,bank);await db.SaveChangesAsync();
    var otherSite=new Web.Data.Entities.Admin.BelongSite{Name="Cơ sở kiểm thử ngoài phạm vi",Code="TEST_OTHER",IsActive=true,CreatedAt=DateTime.UtcNow};db.Add(otherSite);await db.SaveChangesAsync();
    var offsiteEmployee=new Web.Data.Entities.Identity.AppUser{Id=Guid.NewGuid().ToString(),UserName="offsite-test",EmpId="OFFSITE_TEST",FullName="Nhân viên cơ sở khác",Avatar="",SiteId=otherSite.Id,IsActive=true};db.Add(offsiteEmployee);await db.SaveChangesAsync();
    var types=await db.RequestTypes.ToDictionaryAsync(x=>x.TemplateCode);
    var business=new RequestBusinessService(db);
    ApprovalRequest New(string code,string suffix)=>new(){ApplicationNo="TEST-"+suffix,RequestTypeId=types[code].Id,ApprovalCaseId=types[code].ApprovalCaseId,CreatedByUserId=employee.Id,SiteId=employee.SiteId,DepartmentId=1,Title="Kiểm thử "+code,Content="Nội dung nghiệp vụ",Reason="Lý do đề nghị",FaxCode=employee.UserName,FullName=employee.FullName,SectionId=1,SectionName="IT General",DeptId=1,DeptName="Công nghệ thông tin",Email="test@example.invalid",FormCode=code,RequestName=types[code].Name,RequestedDate=new DateTime(2026,10,20),Status=RequestStatus.Completed};
    async Task<ApprovalRequest> Save(string code,string suffix,RequestBusinessDataDto input){var request=New(code,suffix);if(RequestFormCatalog.Get(code).Fields.Any(x=>x.Name=="WorkSiteId"&&x.Required))input.WorkSiteId??=1;await business.SaveAsync(request,types[code],input,true,default);db.Add(request);await db.SaveChangesAsync();return request;}
    var tripInput=new RequestBusinessDataDto {EmployeeId=employee.Id,ReplacementEmployeeId=manager.Id,PurposeId=11,CountryId=21,CityId=33,StartAt=new(2026,10,20),EndAt=new(2026,10,25),CurrencyId=41,ExchangeRate=180,Duration=999,OriginalAmount=1,ParticipantIds=[employee.Id,manager.Id,manager.Id],Items=[new(){CatalogItemId=50,Quantity=1,UnitPrice=150000},new(){CatalogItemId=51,Quantity=5,UnitPrice=24000}]};
    var trip=await Save("BUSINESS_TRIP","TRIP",tripInput);
    Check(trip.BusinessData.Duration==6 && trip.BusinessData.OriginalAmount==270000 && trip.Amount==48600000,"Trip days and costs must be computed from typed source data.");
    Check(trip.BusinessData.Participants.Count==2,"Participants must be unique and include the primary employee.");
    var tripDto=new RequestDetailsDto();await business.PopulateDetailsAsync(tripDto,trip.BusinessData,default);
    Check(tripDto.BusinessFields.Any(x=>x.Value=="Tokyo") && tripDto.BusinessItems.Count==2 && tripDto.ParticipantNames.Count==2,"Details must resolve master names and all business rows.");
    var lookupController=new RequestFormApiController(db){ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"employee1")],"Test"))}}};
    var lookupResult=(OkObjectResult)await lookupController.Lookups(default);var lookupJson=JsonSerializer.SerializeToElement(lookupResult.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    Check(!lookupJson.GetProperty("employees").EnumerateArray().Any(x=>x.GetProperty("id").GetString()==offsiteEmployee.Id),"Employee lookups must respect the existing site boundary.");
    var crossSite=RequestBusinessService.ToInput(trip.BusinessData);crossSite.EmployeeId=offsiteEmployee.Id;
    await Invalid(()=>business.SaveAsync(New("BUSINESS_TRIP","CROSS-SITE"),types["BUSINESS_TRIP"],crossSite,true,default),"Forged cross-site employee IDs must be rejected.");
    var badCity=RequestBusinessService.ToInput(trip.BusinessData);badCity.CityId=30;
    await Invalid(()=>business.SaveAsync(New("BUSINESS_TRIP","BAD-CITY"),types["BUSINESS_TRIP"],badCity,true,default),"Cross-country cities must be rejected.");
    var wrongMaster=RequestBusinessService.ToInput(trip.BusinessData);wrongMaster.PurposeId=50;
    await Invalid(()=>business.SaveAsync(New("BUSINESS_TRIP","BAD-MASTER"),types["BUSINESS_TRIP"],wrongMaster,true,default),"Wrong-category master IDs must be rejected.");
    var leave=await Save("LEAVE","LEAVE",new(){EmployeeId=employee.Id,ReplacementEmployeeId=manager.Id,LeaveTypeId=1,StartAt=new(2026,10,20),EndAt=new(2026,10,21),StartSession="AM",EndSession="AM",CountryId=21});
    Check(leave.BusinessData.Duration==1.5m && leave.BusinessData.CountryId==null,"Leave must calculate half-days and discard fields belonging to other forms.");
    var purchase=await Save("PURCHASE","PURCHASE",new(){TargetDepartmentId=1,PartnerId=partner.Id,Location="Văn phòng",CurrencyId=40,ExchangeRate=123,Items=[new(){CatalogItemId=60,Quantity=2,UnitPrice=12500000}]});
    Check(purchase.Amount==25000000 && purchase.BusinessData.ExchangeRate==1,"VND must always use exchange rate one.");
    var paymentInput=new RequestBusinessDataDto{PartnerId=partner.Id,PaymentMethodId=90,Payee="Đối tác kiểm thử",CurrencyId=40,ExchangeRate=1,InvoiceNumber="TEST-INV",InvoiceDate=new(2026,10,15),Items=[new(){CatalogItemId=54,Quantity=1,UnitPrice=1000000}]};
    await Invalid(()=>business.SaveAsync(New("PAYMENT","BAD-BANK"),types["PAYMENT"],paymentInput,true,default),"Transfers must require a bank and account.");
    paymentInput.BankAccount="TEST-ACCOUNT";paymentInput.BankId=bank.Id;await Save("PAYMENT","PAYMENT",paymentInput);
    await Save("EQUIPMENT","EQUIPMENT",new(){EmployeeId=employee.Id,TargetDepartmentId=1,Location="Văn phòng",Items=[new(){CatalogItemId=70,Quantity=1},new(){CatalogItemId=72,Quantity=2}]});
    await Invalid(()=>business.SaveAsync(New("EQUIPMENT","BAD-QUANTITY"),types["EQUIPMENT"],new(){EmployeeId=employee.Id,WorkSiteId=1,TargetDepartmentId=1,Location="VP",Items=[new(){CatalogItemId=70,Quantity=.5m}]},true,default),"Device quantities must be whole numbers.");
    var overtime=await Save("OVERTIME","OVERTIME",new(){TargetDepartmentId=1,OvertimeTypeId=80,Location="Văn phòng",StartAt=new(2026,10,20,18,0,0),EndAt=new(2026,10,20,22,0,0),ParticipantIds=[employee.Id,manager.Id]});
    Check(overtime.BusinessData.Duration==4,"Overtime must store real hours.");
    await Save("INTERNAL","INTERNAL",new(){TargetDepartmentId=1,ProposalCategoryId=100,ExpectedBenefit="Giảm thời gian xử lý"});
    var vehicleInput=new RequestBusinessDataDto{EmployeeId=employee.Id,ContactPhone="0123456789",VehicleTypeId=111,VehicleId=vehicle.Id,PassengerCount=1,PickupLocation="Văn phòng",Destination="Nhà máy",StartAt=new(2026,10,20,8,0,0),EndAt=new(2026,10,20,17,0,0),ParticipantIds=[manager.Id]};
    vehicleInput.PassengerCount=2;
    await Invalid(()=>business.SaveAsync(New("VEHICLE","BAD-PASSENGER-COUNT"),types["VEHICLE"],vehicleInput,true,default),"Passenger count must match selected participants even when the contact is different.");
    vehicleInput.PassengerCount=1;
    await Save("VEHICLE","VEHICLE",vehicleInput);
    await Invalid(()=>business.SaveAsync(New("VEHICLE","OVERLAP"),types["VEHICLE"],vehicleInput,true,default),"Overlapping assignments for one vehicle must be rejected.");
    var vehicleType=await db.RequestMasterItems.SingleAsync(x=>x.Id==111); var originalCapacity=vehicleType.Capacity;
    vehicleType.Capacity=1; await db.SaveChangesAsync();
    vehicleInput.VehicleId=null;vehicleInput.PassengerCount=2;vehicleInput.ParticipantIds=[employee.Id,manager.Id];
    await Invalid(()=>business.SaveAsync(New("VEHICLE","CAPACITY"),types["VEHICLE"],vehicleInput,true,default),"Passenger count must respect vehicle capacity.");
    vehicleType.Capacity=originalCapacity; await db.SaveChangesAsync();
    var draft=New("LEAVE","DRAFT");draft.Status=RequestStatus.Draft;await business.SaveAsync(draft,types["LEAVE"],new(),false,default);db.Add(draft);await db.SaveChangesAsync();
    await Invalid(()=>business.SaveAsync(New("LEAVE","INCOMPLETE"),types["LEAVE"],new(),true,default),"Submitting an incomplete form must be rejected.");
    var tripId=trip.Id;db.ChangeTracker.Clear();
    trip=await db.ApprovalRequests.Include(x=>x.BusinessData).ThenInclude(x=>x.Items).Include(x=>x.BusinessData).ThenInclude(x=>x.Participants).SingleAsync(x=>x.Id==tripId);
    var edit=RequestBusinessService.ToInput(trip.BusinessData);edit.Items.RemoveAt(0);edit.ParticipantIds.Remove(manager.Id);await business.SaveAsync(trip,types["BUSINESS_TRIP"],edit,true,default);await db.SaveChangesAsync();
    Check(await db.RequestLineItems.CountAsync(x=>x.RequestId==tripId)==1 && await db.RequestParticipants.CountAsync(x=>x.RequestId==tripId)==1,"Editing must persist removed lines and participants without duplicate keys.");
    var inactive=await db.RequestMasterItems.SingleAsync(x=>x.Id==1);inactive.IsActive=false;await db.SaveChangesAsync();
    await Invalid(()=>business.SaveAsync(New("LEAVE","INACTIVE"),types["LEAVE"],RequestBusinessService.ToInput(leave.BusinessData),false,default),"Inactive masters must be rejected.");
    var context=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"employee1")],"Test"))};
    var reports=new RequestReportsApiController(db){ControllerContext=new ControllerContext{HttpContext=context}};
    var purchaseSummary=(OkObjectResult)await reports.PurchaseSummary(null,null,null,null,RequestStatus.Completed,partner.Id,default);
    var purchaseJson=JsonSerializer.SerializeToElement(purchaseSummary.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    Check(purchaseJson.GetProperty("count").GetInt32()==1 && purchaseJson.GetProperty("totalVnd").GetDecimal()==25000000 &&
        purchaseJson.GetProperty("completedVnd").GetDecimal()==25000000 && purchaseJson.GetProperty("suppliers")[0].GetProperty("name").GetString()==partner.Name,
        "Purchase summary must aggregate the correct supplier and approved VND amount.");
    var emptySummary=(OkObjectResult)await reports.PurchaseSummary(null,null,null,null,RequestStatus.Completed,bank.Id,default);
    Check(JsonSerializer.SerializeToElement(emptySummary.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web)).GetProperty("count").GetInt32()==0,
        "Supplier filtering must apply on the server to purchase summary.");
    var otherContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"offsite-test")],"Test"))};
    var otherReports=new RequestReportsApiController(db){ControllerContext=new ControllerContext{HttpContext=otherContext}};
    var privateSummary=(OkObjectResult)await otherReports.PurchaseSummary(null,null,null,null,null,null,default);
    Check(JsonSerializer.SerializeToElement(privateSummary.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web)).GetProperty("count").GetInt32()==0,
        "Purchase summary must not expose requests outside the existing visibility boundary.");
    var result=(OkObjectResult)await reports.Requests(new(){RequireTotalCount=true,TotalSummary=[new(){Selector="amountVnd",SummaryType="sum"}]},null,null,null,null,null,RequestStatus.Completed,default);
    var json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    Check(json.GetProperty("totalCount").GetInt32()==8,"Reports must query all eight persisted templates.");
    var overtimeRow=json.GetProperty("data").EnumerateArray().Single(x=>x.GetProperty("templateCode").GetString()=="OVERTIME");
    Check(overtimeRow.GetProperty("overtimeHours").GetDecimal()==4 && overtimeRow.GetProperty("overtimePersonHours").GetDecimal()==8,"Overtime reports must distinguish hours and person-hours.");
    var items=(OkObjectResult)await reports.Items(new(){RequireTotalCount=true},"PURCHASE",null,null,null,null,RequestStatus.Completed,default);
    var itemJson=JsonSerializer.SerializeToElement(items.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));
    Check(itemJson.GetProperty("totalCount").GetInt32()==1 && itemJson.GetProperty("data")[0].GetProperty("quantity").GetDecimal()==2,"Line-item report must expose quantities and catalog data.");
    var foreign=New("INTERNAL","FOREIGN");foreign.CreatedByUserId=manager.Id;foreign.FaxCode=manager.UserName;db.Add(foreign);await db.SaveChangesAsync();
    result=(OkObjectResult)await reports.Requests(new(){RequireTotalCount=true},null,null,null,null,null,RequestStatus.Completed,default);
    json=JsonSerializer.SerializeToElement(result.Value,new JsonSerializerOptions(JsonSerializerDefaults.Web));Check(json.GetProperty("totalCount").GetInt32()==8,"Reports must enforce the owner/approver visibility boundary.");
    // Dashboard measures use request cohorts, the display timezone, and the same visibility boundary.
    var dashPending=New("LEAVE","DASH-PENDING");dashPending.Status=RequestStatus.Ongoing;dashPending.Priority=RequestPriority.High;dashPending.CreatedAt=new(2025,2,28,17,30,0,DateTimeKind.Utc);dashPending.SubmittedAt=dashPending.CreatedAt;
    var dashDone=New("LEAVE","DASH-DONE");dashDone.CreatedAt=new(2025,3,3,0,0,0,DateTimeKind.Utc);dashDone.SubmittedAt=dashDone.CreatedAt;dashDone.CompletedAt=dashDone.CreatedAt.AddMinutes(150);
    dashDone.Histories.Add(new(){RequestCode=dashDone.ApplicationNo,Action=ApprovalAction.Approved,ActionByUserId=manager.UserName,PreviousStatus=RequestStatus.Ongoing,NewStatus=RequestStatus.Completed,ActionAt=dashDone.CompletedAt.Value});
    var dashReturned=New("LEAVE","DASH-RETURNED");dashReturned.Status=RequestStatus.Returned;dashReturned.CreatedAt=new(2025,3,10,0,0,0,DateTimeKind.Utc);
    dashReturned.Histories.Add(new(){RequestCode=dashReturned.ApplicationNo,Action=ApprovalAction.Returned,ActionByUserId=manager.UserName,PreviousStatus=RequestStatus.Ongoing,NewStatus=RequestStatus.Returned,ActionAt=dashReturned.CreatedAt.AddHours(1)});
    var dashForeign=New("LEAVE","DASH-FOREIGN");dashForeign.CreatedByUserId=manager.Id;dashForeign.FaxCode=manager.UserName;dashForeign.CreatedAt=new(2025,3,4,0,0,0,DateTimeKind.Utc);
    for(var i=0;i<5;i++)dashForeign.Histories.Add(new(){RequestCode=dashForeign.ApplicationNo,Action=ApprovalAction.Approved,ActionByUserId=manager.UserName,ActionAt=dashForeign.CreatedAt.AddHours(i),PreviousStatus=RequestStatus.Ongoing,NewStatus=RequestStatus.Completed});
    var dashAssigned=New("LEAVE","DASH-ASSIGNED");dashAssigned.CreatedByUserId=manager.Id;dashAssigned.FaxCode=manager.UserName;dashAssigned.Status=RequestStatus.Ongoing;dashAssigned.CreatedAt=new(2025,3,5,0,0,0,DateTimeKind.Utc);
    dashAssigned.ApprovalDetails.Add(new(){ApprovalStepId="STEP-ONE-1",StepOrder=1,StepTitle="Trưởng phòng",IsCurrentStep=true,ApproverInfos=new List<ApproverInfo>{new(){UserName=employee.UserName,FullName=employee.FullName}}});
    var dashRemoved=New("LEAVE","DASH-REMOVED");dashRemoved.Status=RequestStatus.Ongoing;dashRemoved.IsRemoved=true;dashRemoved.CreatedAt=new(2025,3,7,0,0,0,DateTimeKind.Utc);
    var dashPrevious=New("LEAVE","DASH-PREVIOUS");dashPrevious.CreatedAt=new(2025,2,20,0,0,0,DateTimeKind.Utc);dashPrevious.SubmittedAt=dashPrevious.CreatedAt;dashPrevious.CompletedAt=dashPrevious.CreatedAt.AddHours(1);
    db.AddRange(dashPending,dashDone,dashReturned,dashForeign,dashAssigned,dashRemoved,dashPrevious);await db.SaveChangesAsync();
    var overview=new ApprovalDashboardOverviewService(db);
    var dash=await overview.GetAsync(employee.UserName,false,new(2025,3,1),new(2025,3,31),3,null);
    Check(dash.Current.Total==4 && dash.Current.Pending==2 && dash.Current.Completed==1 && dash.Current.Returned==1,"Dashboard counts must include owner/approver access and exclude removed or inaccessible requests.");
    Check(dash.Current.AverageHours==2.5 && dash.Current.TimedCompletions==1 && dash.Current.CompletionRate==25,"Dashboard time and completion measures must use real completion timestamps.");
    Check(dash.Previous.Total==1 && dash.Trend.Count==3 && dash.Trend[0].Total==0 && dash.Trend[1].Total==1 && dash.Trend[2].Total==4,"Dashboard must zero-fill months, compare equal-length periods, and bucket UTC dates at UTC+7.");
    Check(dash.ApprovalActions==1 && dash.Approvers.Count==1 && dash.Approvers[0].Count==1 && dash.Activities.Count==2,"Dashboard activity and approver rankings must not leak hidden request histories.");
    Check(dash.Attention.Count==3 && dash.Statuses.Sum(x=>x.Count)==dash.Current.Total && dash.Categories.Sum(x=>x.Count)==4 && dash.Departments.Sum(x=>x.Count)==4,"Dashboard breakdowns and attention rows must match the selected request cohort.");
    Check(dash.AutomaticApprovals==null && dash.AutomationRate==null && dash.SlaRate==null,"Unmeasured automation and SLA metrics must remain unavailable.");
    var adminDash=await overview.GetAsync(employee.UserName,true,new(2025,3,1),new(2025,3,31),3,null);Check(adminDash.Current.Total==5 && adminDash.ApprovalActions==6,"Administrators must see the whole accessible system.");
    var emptyDash=await overview.GetAsync(employee.UserName,false,new(2024,1,1),new(2024,1,31),6,null);Check(emptyDash.Current.Total==0 && emptyDash.Current.AverageHours==null && emptyDash.Current.CompletionRate==null && emptyDash.Trend.All(x=>x.Total==0),"An empty dashboard must not report fabricated percentages or averages.");
    var searchedDash=await overview.GetAsync(employee.UserName,false,new(2025,3,1),new(2025,3,31),3,"DASH-PENDING");Check(searchedDash.Current.Total==1 && searchedDash.Attention.Count==1,"Dashboard search must apply to counts, trends and attention rows.");
    try { await overview.GetAsync(employee.UserName,false,new(2025,3,31),new(2025,3,1),6,null); throw new Exception("Invalid dashboard date ranges must be rejected."); } catch(ArgumentException) { }
    Console.WriteLine("PASS: SQL dashboard aggregation, trend buckets, comparison periods, search, empty states and access boundaries.");
    await WorkflowVersionsVerification.RunAsync(db);
    await OperationsVerification.RunAsync(db);
    await UniversitySeedVerification.RunAsync(db);
    await AnnualLeaveSeedVerification.RunAsync(db);
    await SectionHeadRoutingVerification.RunAsync(db);
    await ReturnedRequestHistoryVerification.RunAsync(db);
    Console.WriteLine("PASS: SQL Server migrations, all eight templates, persistence and edit round-trips.");
    Console.WriteLine("PASS: Master validation, half-days, costs, overtime hours, vehicle capacity and schedule conflicts.");
    Console.WriteLine("PASS: SQL report queries, separate measures, line-item data and request visibility.");
}
finally {
    // Only remove the unique disposable database created by this invocation.
    if(created && db.Database.GetDbConnection().Database==testName && testName.StartsWith("Codex_RequestForms_Test_"))await db.Database.EnsureDeletedAsync();
}










