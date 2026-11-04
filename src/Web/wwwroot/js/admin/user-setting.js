import {
  DATE_FORMAT,
  buildPortalFileOpenUrl,
} from "../common/constants/constants.js?v=1.0.1";
import { formatDateTimeForServer } from "../common/format-data-helper.js";

const t = k => (window.usI18n && window.usI18n[k]) || k;

let settingForm;
let userName = $("#userName").val();
let userStore;
const options = { timeZone: "Asia/Ho_Chi_Minh" };
const editPermission = true;

let _currentOnlineStatus = null; // To track the current online/offline status of the user

$(async () => {
  userStore = await getUserStore();
  userStore.data.inactiveComment = "";
  userStore.data.inactivePeriod = [null, null];

  _currentOnlineStatus = userStore?.data?.isOnline;
  settingForm = $("#setting-form")
    .dxForm({
      formData: userStore.data,
      items: [
        {
          itemType: "group",
          cssClass: "form-group",
          colCount: 3,
          items: [
            {
              template: function () {
                const avatar = userStore.data.avatar;
                const avatarUrl = avatar
                  ? buildPortalFileOpenUrl(avatar)
                  : "/images/default-user.jpg";

                const labelText = editPermission
                  ? t("photo_click")
                  : t("photo_label");
                const hintStyle = editPermission ? "" : "display: none;";
                const cursorStyle = editPermission
                  ? "cursor: pointer;"
                  : "cursor: default; pointer-events: none;";
                const inputDisabled = editPermission ? "" : "disabled";

                // Plain HTML only — do not paste DevExtreme internal classes (dx-responsivebox, dx-box, …)
                // into a template item; on window resize the form's layout manager walks those nodes and throws "box" undefined.
                return `
                  <div class="user-setting-avatar-block">
                    <span class="dx-form-group-caption" style="${hintStyle}">${labelText}</span>
                    <div style="display:flex;justify-content:center;width:100%;">
                      <label style="${cursorStyle}">
                        <input type="file" id="imageInput" style="display: none;" accept="image/*" ${inputDisabled} />
                        <img id="imageOverlay" src="${avatarUrl}" alt="${t("photo_label")}"
                          class="mt-3 img-thumbnail rounded"
                          width="200" height="170"
                          style="object-fit: cover;" />
                      </label>
                    </div>
                  </div>
                `;
              },
            },

            //{
            //    template: function (data, itemElement) {
            //        itemElement.append($("<div>").attr("id", "dxfu1").dxFileUploader());
            //    },
            //    dataField: "fileName",
            //    editorType: 'dxFileUploader',
            //    editorOptions: {
            //        //selectButtonText: 'Select photo',
            //        //labelText: '',
            //        //accept: 'image/*',
            //        dialogTrigger: '#dropzone-external',
            //        dropZone: '#dropzone-external',
            //        multiple: false,
            //        visible: false,
            //    },
            //    label: {
            //        template: 'Profile photo'
            //    }
            //},
            {
              itemType: "group",
              colSpan: 2,
              caption: t("grp_personal"),
              colCount: 3,
              items: [
                {
                  dataField: "empId",
                  editorType: "dxTextBox",

                  editorOptions: {
                    placeholder: t("lbl_empid"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_empid"),
                  },
                },
                {
                  dataField: "userName",
                  editorType: "dxTextBox",

                  editorOptions: {
                    placeholder: t("lbl_username"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_username"),
                  },
                },
                {
                  dataField: "fullName",
                  editorType: "dxTextBox",
                  validationRules: [
                    {
                      type: "required",
                    },
                  ],
                  editorOptions: {
                    readOnly: true,
                    placeholder: t("lbl_fullname"),
                  },
                  label: {
                    template: t("lbl_fullname"),
                  },
                },
                {
                  dataField: "phoneNumber",
                  editorType: "dxTextBox",
                  editorOptions: {
                    placeholder: t("lbl_phone"),
                  },
                  label: {
                    template: t("lbl_phone"),
                  },
                },
                {
                  dataField: "dateOfBirth",
                  editorType: "dxDateBox",
                  editorOptions: {
                    placeholder: DATE_FORMAT,
                    useMaskBehavior: true,
                    displayFormat: DATE_FORMAT,
                  },
                  label: {
                    template: t("lbl_dob"),
                  },
                },
                {
                  itemType: "simple",
                  dataField: "changePassword",
                  editorType: "dxCheckBox",
                  label: {
                    template: labelTemplate("", ""),
                  },
                  editorOptions: {
                    text: t("lbl_change_pwd"),
                    value: false,
                    onValueChanged: function (e) {
                      settingForm.option(
                        "items[0].items[1].items[6].visible",
                        e.value,
                      );
                      settingForm.option(
                        "items[0].items[1].items[7].visible",
                        e.value,
                      );
                      settingForm.option(
                        "items[0].items[1].items[8].visible",
                        e.value,
                      );
                      settingForm.option(
                        "items[0].items[1].items[5].editorOptions.value",
                        e.value,
                      );
                    },
                  },
                },
                {
                  dataField: "currentPass",
                  name: "currentPass",
                  editorType: "dxTextBox",
                  visible: false,
                  editorOptions: {
                    mode: "password",
                    placeholder: t("lbl_current_pwd"),
                    showClearButton: true,
                  },
                  label: {
                    template: t("lbl_current_pwd"),
                  },
                },
                {
                  dataField: "newPass",
                  name: "newPass",
                  editorType: "dxTextBox",
                  visible: false,
                  editorOptions: {
                    mode: "password",
                    placeholder: t("lbl_new_pwd"),
                    showClearButton: true,
                  },
                  label: {
                    template: t("lbl_new_pwd"),
                  },
                },
                {
                  dataField: "confirmPass",
                  name: "confirmPass",
                  editorType: "dxTextBox",
                  visible: false,
                  editorOptions: {
                    mode: "password",
                    placeholder: t("lbl_confirm_pwd"),
                    showClearButton: true,
                  },
                  label: {
                    template: t("lbl_confirm_pwd"),
                  },
                },
              ],
            },
            {
              itemType: "group",
              captionTemplate: groupCaptionTemplate("info"),
              caption: t("grp_work"),
              colSpan: 3,
              colCount: 3,
              items: [
                {
                  dataField: "belongSiteName",
                  editorType: "dxTextBox",
                  editorOptions: {
                    readOnly: true,
                    placeholder: t("lbl_site"),
                  },
                  label: {
                    template: t("lbl_site"),
                  },
                },
                {
                  dataField: "departmentName",
                  editorType: "dxTextBox",

                  editorOptions: {
                    placeholder: t("lbl_dept"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_dept"),
                  },
                },
                {
                  dataField: "sectionName",
                  editorType: "dxTextBox",

                  editorOptions: {
                    placeholder: t("lbl_section"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_section"),
                  },
                },
                {
                  dataField: "position",
                  editorType: "dxTextBox",

                  editorOptions: {
                    placeholder: t("lbl_position"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_position"),
                  },
                },
                {
                  dataField: "email",
                  editorType: "dxTextBox",
                  validationRules: [
                    {
                      type: "email",
                    },
                  ],
                  editorOptions: {
                    placeholder: t("lbl_email"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_email"),
                  },
                },
              ],
            },
            {
              itemType: "group",
              captionTemplate: groupCaptionTemplate("info"),
              caption: t("grp_online"),
              colSpan: 3,
              colCount: 4,
              items: [
                {
                  dataField: "isOnline",
                  editorType: "dxSwitch",
                  editorOptions: {
                    readOnly: false,
                    onValueChanged: function (e) {
                      // Only show offline settings when switching to offline;
                      // when switching back to online, hide and clear offline settings
                      settingForm.option(
                        "items[0].items[4].items[0].editorOptions.readOnly",
                        e.value,
                      );
                      settingForm.option(
                        "items[0].items[4].items[1].editorOptions.readOnly",
                        e.value,
                      );
                      settingForm.option(
                        "items[0].items[4].items[2].editorOptions.readOnly",
                        e.value,
                      );
                    },
                  },
                  label: {
                    template: t("lbl_online"),
                  },
                },
              ],
            },
            {
              itemType: "group",
              //captionTemplate: groupCaptionTemplate("info"),
              colSpan: 3,
              colCount: 3,
              items: [
                {
                  editorType: "dxDateBox",
                  dataField: "offlineFrom",
                  editorOptions: {
                    placeholder: "MM/dd/yyyy HH:mm",
                    readOnly: true,
                    displayFormat: "MM/dd/yyyy HH:mm",
                    type: "datetime",
                  },
                  label: {
                    template: t("lbl_offline_from"),
                  },
                },
                {
                  editorType: "dxDateBox",
                  dataField: "offlineTo",
                  editorOptions: {
                    placeholder: "MM/dd/yyyy HH:mm",
                    readOnly: true,
                    displayFormat: "MM/dd/yyyy HH:mm",
                    type: "datetime",
                  },
                  label: {
                    template: t("lbl_offline_to"),
                  },
                },
                {
                  dataField: "inactiveComment",
                  editorType: "dxTextBox",
                  editorOptions: {
                    placeholder: t("lbl_offline_comment"),
                    readOnly: true,
                  },
                  label: {
                    template: t("lbl_offline_comment"),
                  },
                },
              ],
            },
          ],
        },
      ],
    })
    .dxForm("instance");

  let btnSave = $("#btnSave").dxButton({
    text: t("btn_save"),
    type: "success",
    width: 100,
    //icon: "file",
    onClick: function () {
      let isValid = settingForm.validate().isValid;
      if (isValid) {
        submitForm();
      }
    },
  });

  // Get references to the input and overlay image elements
  const imageInput = document.getElementById("imageInput");
  const imageOverlay = document.getElementById("imageOverlay");

  // Add a click event listener to the overlay image
  // imageOverlay.addEventListener("click", () => {
  //   imageInput.click(); // Programmatically trigger the file input
  // });

  // Add an event listener to the input element (similar to the previous example)
  imageInput.addEventListener("change", (event) => {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        // Set the image source to the data URL
        imageOverlay.src = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  });
});

function submitForm() {
  const formData = settingForm.option("formData");

  void (async () => {
    // Update user status (online/offline) first, as it may affect the validation rules of other fields and the ability to change password
    const ok = await updateUserStatusAsync(formData);
    if (!ok) return;

    // 1) "Change password" → ChangePasswordAsync via API (separate from UpdateAsync)
    if (formData.changePassword) {
      if ((formData.newPass ?? "") !== (formData.confirmPass ?? "")) {
        Swal.fire({
          title: t("swal_warning"),
          text: t("pwd_mismatch"),
          icon: "warning",
        });
        return;
      }
      if (!formData.currentPass || !formData.newPass) {
        Swal.fire({
          title: t("swal_warning"),
          text: t("pwd_required"),
          icon: "warning",
        });
        return;
      }
      try {
        await $.ajax({
          url: "/api/users/change-password",
          method: "POST",
          contentType: "application/json",
          data: JSON.stringify({
            currentPassword: formData.currentPass,
            newPassword: formData.newPass,
          }),
        });
      } catch (xhr) {
        const msg =
          xhr?.responseJSON?.message ??
          (typeof xhr?.responseText === "string" ? xhr.responseText : null) ??
          t("change_pwd_failed");
        Swal.fire({
          title: t("swal_warning"),
          text: msg,
          icon: "warning",
        });
        return;
      }
    }

    // 2) Update profile + avatar → AccountService.UpdateAsync via PUT api/admin/users/{userName}
    const skipKeys = new Set([
      "changePassword",
      "currentPass",
      "newPass",
      "confirmPass",
      "req",
      "isOnline",
      "offlineFrom",
      "offlineTo",
      "inactiveComment",
    ]);
    const form_data = new FormData();
    for (const key in formData) {
      if (skipKeys.has(key)) continue;
      if (key === "dob" || key === "onBoardDay" || key === "signContractDay") {
        form_data.append(
          key,
          new Date(formData[key]).toLocaleString("en-US", options),
        );
      } else if (key === "dateOfBirth" && formData[key]) {
        const d =
          formData[key] instanceof Date
            ? formData[key]
            : new Date(formData[key]);
        if (!Number.isNaN(d.getTime())) {
          form_data.append("dateOfBirth", formatDateTimeForServer(d));
        }
      } else if (
        formData[key] !== undefined &&
        formData[key] !== null &&
        formData[key] !== ""
      ) {
        form_data.append(key, formData[key]);
      }
    }

    form_data.append("userName", userName);

    const imageInput = document.getElementById("imageInput");
    if (imageInput?.files?.length > 0) {
      form_data.append("avatar", imageInput.files[0]);
    }

    const updateUrl = "/api/users/profile";

    try {
      const result = await $.ajax({
        url: updateUrl,
        method: "PUT",
        data: form_data,
        processData: false,
        contentType: false,
      });

      Swal.fire({
        title: t("swal_success"),
        text: result?.message ?? t("save_success"),
        icon: "success",
      }).then(() => window.location.reload());
    } catch (xhr) {
      const msg =
        xhr?.responseJSON?.errors?.join?.("\n") ??
        xhr?.responseJSON?.message ??
        t("save_error");
      Swal.fire({
        title: t("swal_error"),
        text: msg,
        icon: "error",
      });
    }
  })();
}

async function updateUserStatusAsync(formData) {
  if (formData.isOnline === _currentOnlineStatus) {
    // No change in online status, no need to call API
    return true;
  }

  // validate offline settings fields
  if (
    formData.isOnline === false &&
    (!formData.offlineFrom || !formData.offlineTo || !formData.inactiveComment)
  ) {
    await Swal.fire({
      title: t("swal_warning"),
      text: t("offline_required"),
      icon: "warning",
    });
    return false;
  }

  if (formData.offlineFrom && formData.offlineTo) {
    // from must be before to
    const from = new Date(formData.offlineFrom);
    const to = new Date(formData.offlineTo);
    if (from >= to) {
      await Swal.fire({
        title: t("swal_warning"),
        text: t("offline_invalid_range"),
        icon: "warning",
      });
      return false;
    }

    if (to < new Date()) {
      await Swal.fire({
        title: t("swal_warning"),
        text: t("offline_future"),
        icon: "warning",
      });
      return false;
    }
  }

  const payload = {
    goInactive: formData.isOnline === false,
    from: formData.offlineFrom
      ? formatDateTimeForServer(new Date(formData.offlineFrom))
      : null,
    to: formData.offlineTo
      ? formatDateTimeForServer(new Date(formData.offlineTo))
      : null,
    comment: formData.inactiveComment ?? null,
  };

  try {
    const result = await fetch(`/api/user/status`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });
    if (!result || result.status !== 200) {
      await Swal.fire({
        title: t("swal_error"),
        text: t("status_error"),
        icon: "error",
      });
      return false;
    }
    return true;
  } catch (error) {
    await Swal.fire({
      title: t("swal_error"),
      text: t("status_error2"),
      icon: "error",
    });
    return false;
  }
}

function labelTemplate(iconName, label) {
  return (data) =>
    $(`<div><i class='dx-icon dx-icon-${iconName}'></i>${label}</div>`);
}

function groupCaptionTemplate($icon) {
  return (data) => {
    return $(`<i class='mdi mdi-${$icon}'></i><span>${data.caption}</span>`);
  };
}

async function getUserStore() {
  let result = await $.ajax({
    url: "/api/users/detail?userName=" + userName,
    method: "GET",
    contentType: "application/json",
  });

  return result;
}
