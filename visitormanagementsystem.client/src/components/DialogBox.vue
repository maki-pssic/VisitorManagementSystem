<template>
  <teleport to="body">
    <div v-if="show" class="dialog-overlay" @click.self="$emit('close')">
      <div class="dialog-box auth-box-style" :class="{ 'preview-mode': isHtml }">
        <h3 class="dialog-title-style">{{ title }}</h3>

        <div v-if="message" class="dialog-message-container">
          <div v-if="isHtml" v-html="message"></div>

          <div v-else>
            <div class="dialog-message-line"
                 v-for="(line, index) in message.split('\n')"
                 :key="index">
              {{ line }}
            </div>
          </div>
        </div>

        <slot></slot>

        <button v-if="!$slots.default" @click="$emit('close')" class="dialog-btn auth-btn-style">
          Close
        </button>
      </div>
    </div>
  </teleport>
</template>

<script setup>
  import { computed } from 'vue';

  const props = defineProps({
    show: Boolean,
    title: String,
    message: String
  });

  defineEmits(['close']);

  // Check if message starts with HTML to decide rendering mode
  const isHtml = computed(() => {
    return props.message && (props.message.trim().startsWith('<') || props.message.includes('<iframe'));
  });
</script>

<style scoped>
  /* --- Overlay --- */
  .dialog-overlay {
    position: fixed;
    inset: 0;
    background: rgba(19, 34, 56, 0.85);
    display: flex;
    align-items: center; /* Vertical center */
    justify-content: center; /* Horizontal center */
    z-index: 9999;
    backdrop-filter: blur(4px);
    /* Crucial for mobile safe areas */
    padding: env(safe-area-inset-top) env(safe-area-inset-right) env(safe-area-inset-bottom) env(safe-area-inset-left);
  }

  /* --- Main Box --- */
  .auth-box-style {
    background: #ffffff;
    border-radius: 12px;
    box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    border: 1px solid #d1d5db;
    padding: 2rem; /* Using rem to respect your global zoom scaling */
    width: 90%;
    max-width: 500px;
    /* FIX: Prevents overshoot on all phones */
    max-height: 85dvh;
    display: flex;
    flex-direction: column;
    transition: all 0.3s ease;
  }

  .preview-mode {
    max-width: 900px !important;
    width: 95% !important;
  }

  /* --- Title --- */
  .dialog-title-style {
    /* Slightly smaller for mobile scaling */
    font-size: 1.5rem;
    font-weight: 700;
    margin-bottom: 1rem;
    color: #002a57;
    text-align: left;
    border-bottom: 2px solid #e5e7eb;
    padding-bottom: 0.75rem;
    flex-shrink: 0; /* Ensures title doesn't disappear */
  }

  /* --- Container (The Scrollable Bit) --- */
  .dialog-message-container {
    margin: 10px 0;
    text-align: left;
    /* FIX: This makes the message scrollable so the button stays visible */
    overflow-y: auto;
    flex-grow: 1;
    -webkit-overflow-scrolling: touch;
    padding-right: 5px; /* Tiny gap for scrollbar */
  }

  .dialog-message-line {
    color: #4b5563;
    font-size: 1.05rem;
    line-height: 1.6;
    margin-bottom: 8px;
  }

  /* --- Button --- */
  .auth-btn-style {
    margin-top: 1.5rem;
    padding: 12px 24px;
    border: none;
    border-radius: 6px;
    font-size: 1rem;
    font-weight: 600;
    color: white;
    background: #002a57;
    cursor: pointer;
    transition: background 0.2s;
    display: block;
    margin-left: auto;
    flex-shrink: 0; /* Ensures button is never crushed or hidden */
  }

    .auth-btn-style:hover {
      background: #004085;
    }

  /* Iframe adjustment for mobile */
  :deep(iframe) {
    border-radius: 8px;
    border: 1px solid #d1d5db;
    max-width: 100%; /* Prevents horizontal scroll */
  }

  /* Landscape specifically for iPhone 16 Pro Max */
  @media screen and (max-height: 500px) and (orientation: landscape) {
    .auth-box-style {
      padding: 1rem;
      max-height: 95dvh;
    }

    .dialog-title-style {
      font-size: 1.2rem;
      margin-bottom: 0.5rem;
      padding-bottom: 0.5rem;
    }
  }
</style>
