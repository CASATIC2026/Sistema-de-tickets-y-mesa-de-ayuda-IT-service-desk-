document.addEventListener("DOMContentLoaded", function () {

    const buttons = document.querySelectorAll(".faq-question");

    buttons.forEach(button => {
        button.addEventListener("click", function () {

            const answer = this.nextElementSibling;

            document.querySelectorAll(".faq-answer").forEach(item => {
                if (item !== answer) {
                    item.style.display = "none";
                }
            });

            answer.style.display =
                answer.style.display === "block" ? "none" : "block";
        });
    });

});