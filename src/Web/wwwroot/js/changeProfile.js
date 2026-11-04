$(document).ready(function() {
    $("#file_avt").on("change", function(){
        var reader = new FileReader();
         reader.onload = function () {
           var output = document.getElementById("profile_Avatar");
           output.src = reader.result;
         };
         reader.readAsDataURL($("#file_avt").prop('files')[0]);
    });
});