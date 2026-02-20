<template>
  <router-view />
</template>

<script setup>
  // No need to import components here anymore; the Router handles it!
</script>

<style>
  /* Global overrides */
  html, body {
    margin: 0;
    padding: 0;
    width: 100%;
    /* Use dvh to account for Safari's expanding/shrinking address bar */
    min-height: 100dvh;
    background-color: #f4f7f9;
    /* This makes sure the background color 'bleeds' into the notch area */
    background-attachment: fixed;
    overflow-x: hidden;
  }

  #app {
    width: 100%;
    max-width: 100% !important;
    margin: 0 !important;
    display: flex !important;
    flex-direction: column;
    min-height: 100dvh;
    /* IMPORTANT: Respect the Notch and Home Bar on iPhone 16 Pro Max */
    padding-top: env(safe-area-inset-top) !important;
    padding-bottom: env(safe-area-inset-bottom) !important;
    padding-left: env(safe-area-inset-left) !important;
    padding-right: env(safe-area-inset-right) !important;
    box-sizing: border-box;
  }

  /* Prevent auto-zoom on mobile inputs */
  @media (max-width: 768px) {
    input, select, button {
      font-size: 16px !important;
    }
  }

  /* Fix for Landscape Mode on Phones */
  @media screen and (orientation: landscape) and (max-height: 500px) {
    #app {
      /* In landscape, the side insets are more important for the Dynamic Island */
      padding: 10px env(safe-area-inset-right) !important;
    }
  }
</style>
