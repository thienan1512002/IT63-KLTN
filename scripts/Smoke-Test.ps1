param([string]$BaseUrl = 'http://localhost:5099')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http
if (([uri]$BaseUrl).Host -notin @('localhost', '127.0.0.1')) { throw 'Chỉ chạy smoke test trên ứng dụng localhost nối database test.' }
$clients = [Collections.Generic.List[Net.Http.HttpClient]]::new()
$checks = 0
function Check($Condition, [string]$Name) {
    if (!$Condition) { throw "FAIL: $Name" }
    $script:checks++
    Write-Output "PASS: $Name"
}
function Login([string]$UserName) {
    $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
    $page = Invoke-WebRequest "$BaseUrl/Account/Login" -WebSession $session -UseBasicParsing
    $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
    $logged = Invoke-WebRequest "$BaseUrl/Account/Login" -Method Post -WebSession $session -UseBasicParsing -Body @{
        UserName = $UserName; Password = 'Nmv@1231'; __RequestVerificationToken = $token
    }
    if ($logged.BaseResponse.ResponseUri.AbsolutePath -match 'Login|UpdatePassword') { throw "Không đăng nhập được $UserName" }
    $page = Invoke-WebRequest "$BaseUrl/Requests" -WebSession $session -UseBasicParsing
    $csrf = [regex]::Match($page.Content, 'name="csrf-token" content="([^"]+)"').Groups[1].Value
    $handler = [Net.Http.HttpClientHandler]::new()
    $handler.CookieContainer = $session.Cookies
    $handler.AllowAutoRedirect = $false
    $client = [Net.Http.HttpClient]::new($handler)
    $client.BaseAddress = [uri]($BaseUrl + '/')
    $clients.Add($client)
    return @{ Client = $client; Token = $csrf }
}
function Api($Session, [string]$Path, [string]$Method = 'GET', $Body = $null, $Content = $null, [bool]$Csrf = $true) {
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::new($Method), $Path)
    if ($Csrf) { [void]$request.Headers.TryAddWithoutValidation('RequestVerificationToken', $Session.Token) }
    if ($Content) { $request.Content = $Content }
    elseif ($null -ne $Body) { $request.Content = [Net.Http.StringContent]::new(
        (ConvertTo-Json -InputObject $Body -Depth 10), [Text.Encoding]::UTF8, 'application/json') }
    $response = $Session.Client.SendAsync($request).GetAwaiter().GetResult()
    $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $data = $null
    if ($text -and $response.Content.Headers.ContentType.MediaType -eq 'application/json') { $data = $text | ConvertFrom-Json }
    $result = @{ Status = [int]$response.StatusCode; Data = $data; Text = $text }
    $response.Dispose(); $request.Dispose()
    return $result
}
function Draft($Employee, [int]$Type = 2, [bool]$Attachment = $false) {
    $content = [Net.Http.MultipartFormDataContent]::new()
    $fields = @{ RequestTypeId = "$Type"; Title = "HTTP smoke $([guid]::NewGuid().ToString('N'))";
        Content = 'Đề nghị kiểm thử phê duyệt'; RequestedDate = (Get-Date).ToString('yyyy-MM-dd');
        Amount = '150000'; Reason = 'Kiểm thử'; Priority = '1' }
    foreach ($key in $fields.Keys) { $content.Add([Net.Http.StringContent]::new($fields[$key]), $key) }
    if ($Attachment) {
        $file = [Net.Http.ByteArrayContent]::new([Text.Encoding]::UTF8.GetBytes('%PDF-1.4 smoke test'))
        $file.Headers.ContentType = [Net.Http.Headers.MediaTypeHeaderValue]::new('application/pdf')
        $content.Add($file, 'files', 'smoke.pdf')
    }
    $response = Api $Employee 'api/requests' 'POST' $null $content
    if ($response.Status -ne 201) { throw "Create draft: $($response.Status) $($response.Text)" }
    return $response.Data.id
}
try {
    $employee = Login 'employee1'; $manager = Login 'manager1'; $director = Login 'director1'; $admin = Login 'nmvadmin'
    Check $true 'Đăng nhập 4 tài khoản demo'
    foreach ($path in @('', 'Requests', 'Approvals', 'Workflow/Dashboard', 'Requests/Create')) {
        $response = Api $employee $path
        Check ($response.Status -eq 200) "Razor /$path"
    }
    foreach ($path in @('User', 'Department', 'RequestTypes', 'Workflows', 'Roles')) {
        $response = Api $admin $path
        Check ($response.Status -eq 200) "Admin Razor /$path"
    }
    foreach ($path in @('api/admin/users', 'api/admin/departments', 'api/admin/request-types', 'api/admin/workflows', 'api/admin/roles')) {
        $response = Api $admin $path
        Check ($response.Status -eq 200) "Admin API /$path"
    }
    Check ((Api $employee 'api/admin/users').Status -eq 403) 'Nhân viên không đọc quản trị tài khoản'
    Check ((Api $employee 'api/admin/workflows').Status -eq 403) 'Nhân viên không quản trị workflow'
    foreach ($asset in @('js/common/security.js', 'js/workflow/workflows.js', 'layout_assets/devextreme/dx.all.js')) {
        Check ((Api $employee $asset).Status -eq 200) "Tài nguyên UI /$asset"
    }
    $profile = (Api $employee 'api/users/current-user').Data.data
    $profileForm = [Net.Http.MultipartFormDataContent]::new()
    $profileForm.Add([Net.Http.StringContent]::new($profile.fullName), 'FullName')
    $profileForm.Add([Net.Http.StringContent]::new('SHOULD-NOT-CHANGE'), 'EmpId')
    $profileForm.Add([Net.Http.StringContent]::new('999999'), 'SectionId')
    Check ((Api $employee 'api/users/profile' 'PUT' $null $profileForm).Status -eq 200) 'Cập nhật hồ sơ cá nhân không dùng API quản trị'
    $afterProfile = (Api $employee 'api/users/current-user').Data.data
    Check ($afterProfile.empId -eq $profile.empId -and $afterProfile.sectionId -eq $profile.sectionId) 'Không tự sửa mã nhân viên hoặc phòng ban qua hồ sơ'
    $suffix = [guid]::NewGuid().ToString('N').Substring(0,8)
    $departmentBody = @{ name = "Phòng demo $suffix"; shortName = "SMK$suffix"; description = 'Kiểm thử quản trị'; belongSiteId = 1; mainApprover = 'manager1' }
    $department = Api $admin 'api/admin/departments' 'POST' $departmentBody
    Check ($department.Status -eq 201) 'Thêm phòng ban'
    $departmentId = $department.Data.id
    $departmentBody.description = 'Đã cập nhật'
    Check ((Api $admin "api/admin/departments/$departmentId" 'PUT' $departmentBody).Status -eq 200) 'Cập nhật phòng ban'
    Check ((Api $admin "api/admin/departments/$departmentId/toggle" 'POST').Status -eq 204) 'Khóa phòng ban'
    Check ((Api $admin "api/admin/departments/$departmentId/toggle" 'POST').Status -eq 204) 'Mở khóa phòng ban'
    $section = Api $admin 'api/admin/sections' 'POST' @{ name = "Bộ phận $suffix"; engName = "Section $suffix"; departmentId = $departmentId; belongSiteId = 1 }
    Check ($section.Status -eq 201) 'Tạo bộ phận để gán người dùng vào phòng ban'
    $newUserName = "smoke$suffix"
    $userContent = [Net.Http.MultipartFormDataContent]::new()
    $userFields = @{ UserName = $newUserName; FullName = 'Tài khoản kiểm thử'; Email = "$newUserName@example.test";
        Password = 'Demo@12345'; EmpId = "SMK$suffix"; SectionId = "$($section.Data.id)"; BelongSiteId = '1' }
    foreach ($key in $userFields.Keys) { $userContent.Add([Net.Http.StringContent]::new($userFields[$key]), $key) }
    $newUser = Api $admin 'api/admin/users' 'POST' $null $userContent
    Check ($newUser.Status -eq 201) 'Tạo tài khoản thuộc phòng ban'
    $employeeRole = @((Api $admin 'api/admin/roles').Data | Where-Object name -eq 'Employee')[0]
    Check ((Api $admin "api/admin/users/$newUserName/roles" 'PUT' @{roleIds=@($employeeRole.id)}).Status -eq 200) 'Gán vai trò'
    Check ((Api $admin "api/admin/users/$newUserName" 'PATCH' @{isActive=$false}).Status -eq 200) 'Khóa tài khoản'
    Check ((Api $admin "api/admin/users/$newUserName" 'PATCH' @{isActive=$true}).Status -eq 200) 'Mở khóa tài khoản'
    Check ((Api $admin 'api/admin/roles' 'POST' @{name="Demo.$suffix";displayName="Vai trò demo $suffix"}).Status -eq 200) 'Thêm vai trò'
    $workflowId = "SMOKE-$suffix"
    $workflowBody = @{caseId=$workflowId;name='Quy trình smoke';description='Một bước';isActive=$true;
        steps=@(@{stepOrder=1;name='Người phê duyệt';approverKind=2;approverUserName='director1'})}
    Check ((Api $admin 'api/admin/workflows' 'POST' $workflowBody).Status -eq 200) 'Thêm workflow dùng Core cũ'
    $workflowBody.name = 'Quy trình đã cập nhật'
    Check ((Api $admin "api/admin/workflows/$workflowId" 'PUT' $workflowBody).Status -eq 200) 'Cập nhật cấu hình workflow'
    Check ((Api $admin "api/admin/workflows/$workflowId").Data.steps.Count -eq 1) 'Không đếm bước cấu hình đã bỏ'
    $typeBody = @{code="SMK$suffix";name='Loại yêu cầu smoke';description='Demo';approvalCaseId=$workflowId;
        isActive=$true;requireRequestedDate=$false;requireAmount=$false;requireReason=$false;allowAttachment=$true}
    $typeResult = Api $admin 'api/admin/request-types' 'POST' $typeBody
    Check ($typeResult.Status -eq 200) 'Thêm loại yêu cầu'
    $typeBody.description='Đã cập nhật'
    Check ((Api $admin "api/admin/request-types/$($typeResult.Data.id)" 'PUT' $typeBody).Status -eq 204) 'Cập nhật loại yêu cầu'
    Check ((Api $admin "api/admin/request-types/$($typeResult.Data.id)/toggle" 'POST').Status -eq 204) 'Khóa loại yêu cầu'
    Check ((Api $admin "api/admin/request-types/$($typeResult.Data.id)/toggle" 'POST').Status -eq 204) 'Mở loại yêu cầu'
    Check ((Api $employee 'api/requests/1/submit' 'POST' $null $null $false).Status -eq 400) 'POST thiếu CSRF bị chặn'

    $id = Draft $employee 2 $true
    Check ((Api $employee "api/requests/$id/submit" 'POST').Status -eq 204) 'Gửi workflow nhiều bước'
    $detail = (Api $employee "api/requests/$id").Data
    Check ($detail.steps.Count -eq 2 -and $detail.attachments.Count -eq 1) 'Snapshot 2 bước và tệp liên kết'
    Check ((Api $employee $detail.attachments[0].url).Status -eq 200) 'Tải tệp của yêu cầu được phép xem'
    $outsiderId = Draft $employee 1 $true
    [void](Api $employee "api/requests/$outsiderId/submit" 'POST')
    $private = (Api $employee "api/requests/$outsiderId").Data
    Check ((Api $director $private.attachments[0].url).Status -eq 403) 'Tệp riêng tư không được truy cập ngoài phân công'
    Check ((Api $employee 'Files/requests/1/smoke.pdf').Status -eq 404) 'Không truy cập tệp qua đường static công khai'
    Check ((Api $employee "api/requests/$id/approve" 'POST' @{comment='Tự duyệt'}).Status -eq 400) 'Ngăn tự phê duyệt'
    Check ((Api $director "api/requests/$id/approve" 'POST' @{comment='Sai bước'}).Status -eq 400) 'Ngăn xử lý sai bước'
    Check ((Api $manager "api/requests/$id/approve" 'POST' @{comment='Bước 1'}).Status -eq 204) 'Trưởng phòng phê duyệt'
    Check ((Api $director "api/requests/$id/approve" 'POST' @{comment='Bước 2'}).Status -eq 204) 'Giám đốc phê duyệt'
    $completed = (Api $employee "api/requests/$id").Data
    Check ($completed.status -eq 2 -and $completed.histories.Count -eq 4) 'Hoàn tất và lịch sử đầy đủ'
    Check ((Api $manager "api/requests/$id/approve" 'POST' @{comment='Lặp'}).Status -eq 400) 'Không duyệt lại bước hoàn thành'
    $returnedId = Draft $employee 1
    [void](Api $employee "api/requests/$returnedId/submit" 'POST')
    Check ((Api $manager "api/requests/$returnedId/return" 'POST' @{comment=''}).Status -eq 400) 'Trả lại bắt buộc có ý kiến'
    Check ((Api $manager "api/requests/$returnedId/return" 'POST' @{comment='Bổ sung'}).Status -eq 204) 'Trả lại yêu cầu'
    Check ((Api $employee "api/requests/$returnedId/submit" 'POST').Status -eq 204) 'Gửi lại snapshot cũ'
    Check ((Api $manager "api/requests/$returnedId/approve" 'POST' @{comment='Đồng ý'}).Status -eq 204) 'Hoàn tất yêu cầu gửi lại'
    $rejectedId = Draft $employee 1
    [void](Api $employee "api/requests/$rejectedId/submit" 'POST')
    Check ((Api $manager "api/requests/$rejectedId/reject" 'POST' @{comment='Không hợp lệ'}).Status -eq 204) 'Từ chối'
    Check ((Api $employee "api/requests/$rejectedId").Data.status -eq 4) 'Trạng thái đã từ chối'
    $notifications = (Api $employee 'api/notitoast/all').Data
    Check (@($notifications).Count -gt 0) 'Thông báo trong hệ thống'
    Check ((Api $employee "api/notitoast/$($notifications[0].id)/read" 'POST').Status -eq 204) 'Đánh dấu đã đọc'
    Check ((Api $employee 'api/workflow-dashboard/summary').Status -eq 200) 'Dashboard thống kê'
    Check ((Api $employee 'api/workflow-dashboard/report?requireTotalCount=true&take=10').Status -eq 200) 'Báo cáo và phân trang SQL'
    Check ((Api $manager 'api/requests/pending?requireTotalCount=true&take=10').Status -eq 200) 'Danh sách chờ phê duyệt'
    Check ((Api $employee 'Account/Logout' 'POST').Status -eq 302) 'Đăng xuất'
    Check ((Api $employee 'api/requests/mine').Status -eq 401) 'Phiên đã đăng xuất bị chặn'
    Write-Output "SMOKE PASS: $checks checks. Yêu cầu test được giữ trong database test."
} finally { foreach ($client in $clients) { $client.Dispose() } }
