document.addEventListener("DOMContentLoaded", function () {
  const images = document.querySelectorAll(".gallery-item img");
  const lightbox = document.querySelector(".lightbox");
  const lightboxImg = document.querySelector(".lightbox-content");
  const closeBtn = document.querySelector(".close");

  // Khi click vào ảnh -> phóng to
  images.forEach((img) => {
    img.addEventListener("click", () => {
      lightbox.style.display = "flex";
      lightboxImg.src = img.src;
    });
  });

  // Click nút đóng
  closeBtn.addEventListener("click", () => {
    lightbox.style.display = "none";
  });

  // Click ra ngoài ảnh cũng tắt
  lightbox.addEventListener("click", (e) => {
    if (e.target === lightbox) {
      lightbox.style.display = "none";
    }
  });
});
