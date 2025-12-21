
function openNewTab(url) {
   window.open(url, '_blank');
}

window.copyToClipboard = async (text) => {

   // Falls das Dokument keinen Fokus hat
   if (!document.hasFocus()) {
      window.focus();
   }

   try {
      await navigator.clipboard.writeText(text);
      return true;
   }
   catch (e) {
      console.error(e);

      // Fallback
      const textarea = document.createElement("textarea");
      textarea.value = text;
      textarea.style.position = "fixed";
      document.body.appendChild(textarea);
      textarea.focus();
      textarea.select();
      document.execCommand("copy");
      document.body.removeChild(textarea);

      return true;
   }
}