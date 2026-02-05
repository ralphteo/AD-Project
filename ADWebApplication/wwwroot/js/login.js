window.onload = function () {
  var passwordInput = document.getElementById("passwordInput");
  var toggleBtn = document.getElementById("togglePwd");

  if (!passwordInput || !toggleBtn) {
    return;
  }

  toggleBtn.onclick = function () {
    if (passwordInput.type === "password") {
      passwordInput.type = "text";
    } else {
      passwordInput.type = "password";
    }
  };
};
