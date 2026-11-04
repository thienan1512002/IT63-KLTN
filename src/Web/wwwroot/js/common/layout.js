(function () {
  const t = window.i18n || {};
  const labels = {
    userName: t.userName || "User Name",
    fullName: t.fullName || "Full Name",
    email: t.email || "Email",
    phone: t.phone || "Phone",
    dob: t.dob || "DOB",
    department: t.department || "Department",
    section: t.section || "Section",
    position: t.position || "Position",
    site: t.site || "Site",
  };

  function closeSearch() {
    $("#modalSearch").modal("hide");
    $("#searchEmpty").modal("hide");
  }

  function addWrapTextClass() {
    const previewSubjects = document.getElementsByClassName("preview-subject");
    const windowWidth = window.innerWidth;
    for (let i = 0; i < previewSubjects.length; i++) {
      if (windowWidth < 900) {
        previewSubjects[i].classList.add("text-wrap");
      } else {
        previewSubjects[i].classList.remove("text-wrap");
      }
    }
  }

  function formatDateOfBirth(value) {
    if (!value) return "";
    const date = new Date(value);
    if (!Number.isNaN(date.getTime())) {
      const day = String(date.getDate()).padStart(2, "0");
      const month = String(date.getMonth() + 1).padStart(2, "0");
      const year = date.getFullYear();
      return `${day}/${month}/${year}`;
    }
    return String(value);
  }

  function mapBelongSite(site) {
    if (!site) return "";
    return site
      .replace("FVB", "HeadQuater")
      .replace("FV2", "Văn phòng Hà Nội")
      .replace("FVD", "R&D")
      .replace("FVQ", "Evelator Project");
  }

  function renderUserSearchResult(users) {
    const $resultDiv = $("#resultDiv");
    $resultDiv.html("");
    users.forEach(function (value) {
      const avatarPath = value.avatar
        ? `/Files/${value.avatar}`
        : "/images/default-user.jpg";
      $resultDiv.append(
        "<div class='col-md-2 mb-3'>" +
          `<img src='${avatarPath}' id='avatar' height='120px' width='110px'>` +
          "</div>" +
          "<div class='col-lg-5'>" +
          "<dl class='row mb-sm-0'>" +
          `<dt class='col-4'>${labels.userName}:</dt>` +
          "<dd class='col-8'>" +
          (value.userName ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.fullName}:</dt>` +
          "<dd class='col-8'>" +
          (value.fullName ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.email}:</dt>` +
          "<dd class='col-8' id='empEmail'>" +
          (value.email ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.phone}:</dt>` +
          "<dd class='col-8' id='empPhone'>" +
          (value.phoneNumber ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.dob}:</dt>` +
          "<dd class='col-8' id='empDOB'>" +
          formatDateOfBirth(value.dateOfBirth) +
          "</dd>" +
          "</dl>" +
          "</div>" +
          "<div class='col-lg-5'>" +
          "<dl class='row mb-sm-0'>" +
          `<dt class='col-4'>${labels.site}:</dt>` +
          "<dd class='col-8' id='empSite'>" +
          mapBelongSite(value.belongSiteName) +
          "</dd>" +
          `<dt class='col-4'>${labels.department}:</dt>` +
          "<dd class='col-8' id='empDept'>" +
          (value.departmentName ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.section}:</dt>` +
          "<dd class='col-8' id='empSec'>" +
          (value.sectionName ?? "") +
          "</dd>" +
          `<dt class='col-4'>${labels.position}:</dt>` +
          "<dd class='col-8' id='empPos'>" +
          (value.position ?? "") +
          "</dd>" +
          "<dt class='col-4'>Active:</dt>" +
          "<dd class='col-8' id='empActive'>" +
          ((value.isActive ?? false) ? "Active" : "InActive") +
          "</dd>" +
          "</dl>" +
          "</div>" +
          "<hr width='100%'>",
      );
    });
  }

  function searchEmployee() {
    const userName = $("#navbar-search-input").val();
    const encodedUserName = $("<div>").text(userName).html();
    $.ajax({
      url: "/api/users/details",
      method: "GET",
      data: { userName: userName },
      success: function (data) {
        const users = Array.isArray(data) ? data : [];
        if (users.length === 0) {
          $("#searchText").html(encodedUserName);
          $("#aEmpty").click();
          $("#navbar-search-input").val("");
          return;
        }

        renderUserSearchResult(users);
        $("#aSearch").click();
        $("#navbar-search-input").val("");
      },
    });
  }

  function alertPermission() {
    $("#accessModal").modal("show");
  }

  $(document).ready(function () {
    $.ajax({
      type: "GET",
      url: "/api/users/current-user",
      success: function (res) {
        const avatar = res?.data?.avatar;
        const src = avatar ? "/Files/" + avatar : "/images/default-user.jpg";
        $("#avtImg").attr("src", src);
        $("#online-status").attr(
          "style",
          res.data.isOnline
            ? "background-color: #4bb543;"
            : "background-color: #ccc;",
        );
      },
    });

    $.ajax({
      type: "GET",
      url: "/api/notitoast/all",
      success: function (data) {
        if (data) {
          let count = Object.keys(data).length;
          $("#countNoti").text(count);
          if (count == 0) {
            $("#countNoti").css("display", "none");
          }
          //alert(count);
          data.forEach((item) => {
            const link = $("<a>").addClass("dropdown-item preview-item").attr("href", item.url);
            $("<div>").addClass("preview-item-content").append(
              $("<h6>").addClass("preview-subject font-weight-normal").text(item.content)
            ).appendTo(link);
            link.on("click", async function (event) {
              event.preventDefault();
              await fetch("/api/notitoast/" + item.id + "/read", { method: "POST" });
              location.href = item.url;
            });
            $("#notificationDropdown1").append(link);
          });
        }
      },
    });

    $("#clearNotiAll").click(function () {
      $.ajax({
        url: "/api/notitoast/clear",
        type: "DELETE",
        success: function (common) {
          if (common === 200) {
            $("#notificationDropdown1").children("a.preview-item").remove();
            $("#countNoti").css("display", "none");
          }
        },
      });
    });

    $("#tagDownload").click(function () {
      iziToast.show({
        title: "Success",
        message:
          "Please extract all folder into Drive C  and run on totalreg.reg ",
        color: "green",
        icon: "mdi mdi-check",
      });
    });


    const alertResetPasswordModel = $("#resetPasswordModal");
    if (alertResetPasswordModel.length > 0) {
      alertResetPasswordModel.modal("show");
    }

    $("#navbar-search-input").keypress(function (e) {
      if (e.keyCode == 13) {
        searchEmployee();
      }
    });
    $("#search").click(function () {
      searchEmployee();
    });

    // const culture = window.layoutConfig?.culture || "en-US";
    // const dxLocale = culture.toLowerCase().startsWith("vi") ? "vi" : "en";
    // if (window.DevExpress && DevExpress.localization) {
    //   DevExpress.localization.locale(dxLocale);
    // }
  });

  window.closeSearch = closeSearch;
  window.searchEmployee = searchEmployee;
})();
