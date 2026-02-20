<template>
  <div class="registration-wrapper">

    <div v-if="currentStep === 'privacy'" class="privacy-container">
      <div class="privacy-card">
        <h1 class="main-title">VISITOR PRE-REGISTRATION</h1>
        <h3 class="dark-text"> All visitors are required to pre-register prior to their visit. Please complete this form accurately. Information collected will be used solely for security, safety, and visitor management purposes. </h3>

        <div class="privacy-content markdown-body" v-html="privacyHtml"></div>

        <div class="consent-action">
          <div class="checkbox-wrapper">
            <input type="checkbox" id="consent" v-model="form.agreedToPrivacy" />
            <label for="consent" class="dark-text">I acknowledge and consent to the collection, processing, storage, and use of my personal data.</label>
          </div>

          <button @click="proceedToForm"
                  class="proceed-btn"
                  :disabled="!form.agreedToPrivacy">
            Agree and Proceed
          </button>
        </div>
      </div>
    </div>

    <div v-else class="registration-container">
      <div class="form-header-action">
        <div class="header-content">
          <h2 class="form-title">Pre-Registration Form</h2>
          <p class="form-subtitle">Please fill out the details below to schedule your visit.</p>

          <button @click="currentStep = 'privacy'" class="back-link">
            ← Back to Privacy Notice
          </button>
        </div>
      </div>

      <div class="form-card">
        <div v-if="hasErrors && submittedOnce" class="error-summary-box">
          <p>⚠️ Please complete all required fields highlighted below.</p>
        </div>

        <form @submit.prevent="handleSubmit" class="scrollable-form">

          <div class="form-section">
            <label class="section-label">Visit Details</label>
            <div class="logistics-grid">

              <div class="logistics-column">
                <div class="input-field">
                  <label>Select Site*</label>
                  <select v-model="form.branchId"
                          @change="clearError('branchId')"
                          :class="{
                            'input-error': errors.branchId && submittedOnce,
                            'placeholder-state': form.branchId === null
                          }"
                          class="main-select">
                    <option :value="null" disabled selected>-- Please Select a Site --</option>
                    <option :value="1">Tarlac: NPO-PSSIC Security Printing Campus</option>
                    <option :value="2">Quezon City: NPO-PSSIC Identity Cards Center</option>
                  </select>
                  <span v-if="errors.branchId && submittedOnce" class="error-text">{{ errors.branchId }}</span>
                </div>

                <div class="input-group-row">
                  <div class="input-field">
                    <label>Date of Visit*</label>
                    <input type="date"
                           v-model="form.visitDate"
                           :min="new Date().toISOString().split('T')[0]"
                           @change="clearError('visitDate')" />
                    <span v-if="errors.visitDate && submittedOnce" class="error-text">{{ errors.visitDate }}</span>
                  </div>
                  <div class="input-field">
                    <label>Time of Visit*</label>
                    <div class="time-picker-grid">
                      <select v-model="timeParts.hour" class="main-select">
                        <option v-for="h in 12" :key="h" :value="h">{{ h }}</option>
                      </select>

                      <select v-model="timeParts.minute" class="main-select">
                        <option value="00">00</option>
                        <option value="15">15</option>
                        <option value="30">30</option>
                        <option value="45">45</option>
                      </select>

                      <select v-model="timeParts.period" class="main-select">
                        <option value="AM">AM</option>
                        <option value="PM">PM</option>
                      </select>
                    </div>
                    <span v-if="errors.visitTime && submittedOnce" class="error-text">{{ errors.visitTime }}</span>
                  </div>
                </div>
              </div>

              <div class="logistics-column">
                <div class="input-field">
                  <label>Visitee*</label>
                  <input v-model="form.authorizedPersonnel"
                         @input="clearError('authorizedPersonnel')"
                         :class="{ 'input-error': errors.authorizedPersonnel && submittedOnce }"
                         placeholder="Full Name" />
                  <span v-if="errors.authorizedPersonnel && submittedOnce" class="error-text">
                    {{ errors.authorizedPersonnel }}
                  </span>
                </div>

                <div class="input-field">
                  <label>Department*</label>
                  <input v-model="form.department"
                         @input="clearError('department')"
                         :class="{ 'input-error': errors.department && submittedOnce }"
                         placeholder="e.g. IT, HR, Finance" />
                  <span v-if="errors.department && submittedOnce" class="error-text">
                    {{ errors.department }}
                  </span>
                </div>

                <div class="input-field">
                  <label>Purpose of Visit*</label>
                  <textarea v-model="form.purposeOfVisit"
                            @input="clearError('purposeOfVisit')"
                            :class="{ 'input-error': errors.purposeOfVisit && submittedOnce }"
                            placeholder="Please state your reason of visit"
                            rows="3"></textarea>
                  <span v-if="errors.purposeOfVisit && submittedOnce" class="error-text">
                    {{ errors.purposeOfVisit }}
                  </span>
                </div>
              </div>x
            </div>
          </div>

          <div class="form-section">
            <div class="section-flex-header">
              <label class="section-label">Registrant & Visitor Information</label>
            </div>

            <div class="repeater-card registrant-card">
              <div class="card-title-row">
                <span class="visitor-type-label">Primary Registrant (Organizer)</span>
              </div>

              <div class="registrant-options">
                <div class="checkbox-wrapper">
                  <input type="checkbox" id="isAlsoVisitor" v-model="form.registrant.isAlsoVisitor" />
                  <label for="isAlsoVisitor" class="dark-text">Registrant is also a Visitor</label>
                </div>
              </div>

              <div class="input-grid-names">
                <div class="input-field col-prefix"><label>Prefix</label><input v-model="form.registrant.prefix" placeholder="Mr./Ms." /></div>
                <div class="input-field col-main">
                  <label>First Name*</label>
                  <input v-model="form.registrant.firstName" @input="clearError('registrant', 'firstName')" :class="{ 'input-error': errors.registrant?.firstName && submittedOnce }" placeholder="First Name"/>
                </div>
                <div class="input-field col-main"><label>Middle Name</label><input v-model="form.registrant.middleName" placeholder="Middle Name"/></div>
                <div class="input-field col-main">
                  <label>Last Name*</label>
                  <input v-model="form.registrant.lastName" @input="clearError('registrant', 'lastName')" :class="{ 'input-error': errors.registrant?.lastName && submittedOnce }" placeholder="Last Name"/>
                </div>
                <div class="input-field col-suffix"><label>Suffix</label><input v-model="form.registrant.suffix" placeholder="Jr." /></div>
              </div>

              <div class="input-grid-3">
                <div class="input-field">
                  <label>Company / Organization*</label>
                  <input v-model="form.registrant.companyName" @input="clearError('registrant', 'companyName')" :class="{ 'input-error': errors.registrant?.companyName && submittedOnce }" placeholder="Agency/Company" />
                </div>
                <div class="input-field">
                  <label>Designation*</label>
                   <input v-model="form.registrant.designation" @input="clearError('registrant', 'designation')" :class="{ 'input-error': errors.registrant?.companyName && submittedOnce }" placeholder="Position" />
                </div>
                <div class="input-field">
                  <label>Email Address*</label>
                  <input v-model="form.registrant.email" type="email" @input="clearError('registrant', 'email')" :class="{ 'input-error': errors.registrant?.email && submittedOnce }" placeholder="Email Address"/>
                </div>
              </div>

              <div class="input-field mt-3">
                <label>Valid ID*</label>
                <div class="upload-control-group" :class="{ 'upload-error-border': errors.registrant?.validId && submittedOnce }">
                  <input type="file"
                         id="file-registrant"
                         @change="e => { handleFileUpload(e, form.registrant); clearError('registrant', 'validId') }"
                         accept="image/*" hidden />

                  <div v-if="form.registrant.validId" class="id-preview-container">
                    <div class="preview-wrapper">
                      <img :src="form.registrant.validId" class="id-thumbnail" @click="openFullImage(form.registrant.validId, 'Your ID')" />
                      <button type="button" class="remove-file-btn" @click="removeFile(form.registrant)" title="Remove Image">×</button>
                    </div>
                  </div>

                  <button type="button" class="upload-btn" @click="triggerUpload('file-registrant')">
                    {{ form.registrant.validId ? 'Change ID' : 'Upload ID' }}
                  </button>
                </div>
                <span v-if="errors.registrant?.validId && submittedOnce" class="error-text">Identity verification is required</span>
              </div>
            </div>

            <hr class="section-divider" v-if="form.visitors.length > 0" />

            <div v-for="(visitor, index) in form.visitors" :key="index" class="repeater-card">
              <div class="card-title-row">
                <span class="visitor-type-label">Additional Visitor {{ index + 1 }}</span>
                <button type="button" @click="removeVisitor(index)" class="remove-btn">Remove</button>
              </div>

              <div class="input-grid-names">
                <div class="input-field col-prefix"><label>Prefix</label><input v-model="visitor.prefix" placeholder="Mr./Ms." /></div>
                <div class="input-field col-main">
                  <label>First Name*</label>
                  <input v-model="visitor.firstName" @input="clearNestedError('visitors', index, 'firstName')" :class="{ 'input-error': errors.visitors[index]?.firstName && submittedOnce }" placeholder="First Name" />
                </div>
                <div class="input-field col-main">
                  <label>Middle Name</label>
                  <input v-model="visitor.middleName" placeholder="Middle Name" /></div>
                <div class="input-field col-main">
                  <label>Last Name*</label>
                  <input v-model="visitor.lastName" @input="clearNestedError('visitors', index, 'lastName')" :class="{ 'input-error': errors.visitors[index]?.lastName && submittedOnce }" placeholder="Last Name" />
                </div>
                <div class="input-field col-suffix"><label>Suffix</label><input v-model="visitor.suffix" placeholder="Jr." /></div>
              </div>

              <div class="input-grid-3">
                <div class="input-field">
                  <label>Company / Organization*</label>
                  <input v-model="visitor.companyName" @input="clearNestedError('visitors', index, 'companyName')" :class="{ 'input-error': errors.visitors[index]?.companyName && submittedOnce }" placeholder="Agency/Company" />
                </div>
                <div class="input-field">
                  <label>Designation*</label>
                  <input v-model="visitor.designation" @input="clearNestedError('visitors', index, 'designation')" :class="{ 'input-error': errors.visitors[index]?.designation && submittedOnce }" placeholder="Position" /></div>
                <div class="input-field">
                  <label>Email Address*</label>
                  <input v-model="visitor.email" type="email" @input="clearNestedError('visitors', index, 'email')" :class="{ 'input-error': errors.visitors[index]?.email && submittedOnce }" placeholder="Email Address" />
                </div>
              </div>

              <div class="input-field mt-3">
                <label>Valid ID*</label>
                <div class="upload-control-group" :class="{ 'upload-error-border': errors.visitors[index]?.validId && submittedOnce }">
                  <input type="file" :id="'file-v-'+index"
                         @change="e => { handleFileUpload(e, visitor); clearNestedError('visitors', index, 'validId') }"
                         accept="image/*" hidden />

                  <div v-if="visitor.validId" class="id-preview-container">
                    <div class="preview-wrapper">
                      <img :src="visitor.validId" class="id-thumbnail" @click="openFullImage(visitor.validId, 'Visitor ID')" />
                      <button type="button" class="remove-file-btn" @click="removeFile(visitor, 'visitors', index)" title="Remove Image">×</button>
                    </div>
                  </div>

                  <button type="button" class="upload-btn" @click="triggerUpload('file-v-' + index)">
                    {{ visitor.validId ? 'Change ID' : 'Upload ID' }}
                  </button>
                </div>
              </div>
            </div>

            <div class="repeater-actions">
              <button type="button" @click="addVisitor" class="add-visitor-bottom-btn">+ Add Visitor / Companion</button>
            </div>
          </div>

          <div class="form-section">
            <div class="checkbox-wrapper">
              <input type="checkbox" id="carpass" v-model="form.needsCarpass" @change="toggleVehicle" />
              <label for="carpass">I am bringing a vehicle</label>
            </div>

            <div v-show="form.needsCarpass">
              <div v-for="(car, cIdx) in form.vehicles" :key="cIdx" class="repeater-card vehicle-card">
                <div class="card-title-row">
                  <span>Vehicle {{ cIdx + 1 }}</span>
                  <button type="button" @click="removeVehicle(cIdx)" class="remove-btn">Remove Vehicle</button>
                </div>

                <div class="driver-selection-area">
                  <input type="checkbox" :id="'driverCheck'+cIdx" v-model="car.driverIsVisitor" @change="handleDriverToggle(car, cIdx)" />
                  <label :for="'driverCheck'+cIdx">Driver is one of the visitors</label>
                </div>

                <div v-if="!car.driverIsVisitor">
                  <div class="input-grid-3">
                    <div class="input-field">
                      <label>Driver First Name*</label>
                      <input v-model="car.driverFirstName"
                             @input="clearNestedError('vehicles', cIdx, 'driverFirstName')"
                             :class="{ 'input-error': errors.vehicles[cIdx]?.driverFirstName && submittedOnce }"
                             placeholder="First Name" />
                      <span v-if="errors.vehicles[cIdx]?.driverFirstName && submittedOnce" class="error-text">{{ errors.vehicles[cIdx].driverFirstName }}</span>
                    </div>
                    <div class="input-field">
                      <label>Driver Middle Name</label>
                      <input v-model="car.driverMiddleName" placeholder="Middle Name" /></div>
                    <div class="input-field">
                      <label>Driver Last Name*</label>
                      <input v-model="car.driverLastName" @input="clearNestedError('vehicles', cIdx, 'driverLastName')" :class="{ 'input-error': errors.vehicles[cIdx]?.driverLastName && submittedOnce }" placeholder="Last Name" />
                      <span v-if="errors.vehicles[cIdx]?.driverLastName && submittedOnce" class="error-text">{{ errors.vehicles[cIdx].driverLastName }}</span>
                    </div>
                  </div>

                  <div class="input-field">
                    <label>Driver's License*</label>
                    <div class="upload-control-group" :class="{ 'upload-error-border': errors.vehicles[cIdx]?.validId && submittedOnce }">
                      <input type="file" :id="'driver-file-'+cIdx"
                             @change="e => { handleFileUpload(e, car); clearNestedError('vehicles', cIdx, 'validId') }"
                             accept="image/*" hidden />

                      <div v-if="car.validId" class="id-preview-container">
                        <div class="preview-wrapper">
                          <img :src="car.validId" class="id-thumbnail" @click="openFullImage(car.validId, 'License')" />
                          <button type="button"
                                  class="remove-file-btn"
                                  @click="removeFile(car, 'vehicles', cIdx)"
                                  title="Remove Image">
                            ×
                          </button>
                        </div>
                      </div>

                      <button type="button" class="upload-btn" @click="triggerUpload('driver-file-' + cIdx)">
                        {{ car.validId ? 'Change License' : 'Upload License' }}
                      </button>
                    </div>
                    <span v-if="errors.vehicles[cIdx]?.validId && submittedOnce" class="error-text">
                      {{ errors.vehicles[cIdx].validId }}
                    </span>
                  </div>
                </div>

                <div v-else class="input-field">
                  <label>Select Driver*</label>
                  <select v-model="car.selectedVisitorIndex"
                          @change="syncDriver(car.selectedVisitorIndex, cIdx); clearNestedError('vehicles', cIdx, 'selectedVisitorIndex')"
                          :class="{ 'input-error': errors.vehicles[cIdx]?.selectedVisitorIndex && submittedOnce }"
                          class="main-select">
                    <option :value="null">-- Select from Visitors --</option>

                    <option v-if="form.registrant.isAlsoVisitor" value="registrant">
                      {{ form.registrant.firstName }} {{ form.registrant.lastName }} (Registrant)
                    </option>

                    <option v-for="(v, vIdx) in form.visitors" :key="vIdx" :value="vIdx">
                      {{ v.firstName }} {{ v.lastName }}
                    </option>
                  </select>

                  <span v-if="errors.vehicles[cIdx]?.selectedVisitorIndex && submittedOnce" class="error-text">
                    {{ errors.vehicles[cIdx].selectedVisitorIndex }}
                  </span>

                  <div v-if="car.validId" class="id-preview-container synced-preview">
                    <label class="mini-label">Synced Visitor ID:</label>
                    <div class="preview-wrapper">
                      <img :src="car.validId"
                           class="id-thumbnail"
                           @click="openFullImage(car.validId, 'Driver ID: ' + car.driverLastName)" />
                    </div>
                  </div>
                </div>

                <div class="input-grid-3 border-top">
                  <div class="input-field">
                    <label>Plate No.*</label>
                    <input v-model="car.plateNumber"
                           @input="clearNestedError('vehicles', cIdx, 'plateNumber')"
                           :class="{ 'input-error': errors.vehicles[cIdx]?.plateNumber && submittedOnce }"
                           placeholder="ABC 1234" />
                    <span v-if="errors.vehicles[cIdx]?.plateNumber && submittedOnce" class="error-text">{{ errors.vehicles[cIdx].plateNumber }}</span>
                  </div>
                  <div class="input-field">
                    <label>Model/Make*</label>
                    <input v-model="car.model"
                           @input="clearNestedError('vehicles', cIdx, 'model')"
                           :class="{ 'input-error': errors.vehicles[cIdx]?.model && submittedOnce }"
                           placeholder="Toyota Vios" />
                    <span v-if="errors.vehicles[cIdx]?.model && submittedOnce" class="error-text">{{ errors.vehicles[cIdx].model }}</span>
                  </div>
                  <div class="input-field">
                    <label>Color*</label>
                    <input v-model="car.color"
                           @input="clearNestedError('vehicles', cIdx, 'color')"
                           :class="{ 'input-error': errors.vehicles[cIdx]?.color && submittedOnce }"
                           placeholder="White" />
                    <span v-if="errors.vehicles[cIdx]?.color && submittedOnce" class="error-text">{{ errors.vehicles[cIdx].color }}</span>
                  </div>
                </div>
              </div>

              <div class="repeater-actions">
                <button type="button" @click="addVehicle" class="add-visitor-bottom-btn">+ Add Another Vehicle</button>
              </div>
            </div>
          </div>
        </form>

        <div class="form-footer">
          <button @click="handleSubmit" class="submit-btn" :disabled="isLoading">
            {{ isLoading ? 'Submitting...' : 'Submit Registration' }}
          </button>
          <p v-if="isTakingLong" class="wait-warning">Processing images, please don't close this page...</p>
        </div>
      </div>
    </div>
  </div>

  <DialogBox :show="previewDialog.show" :title="previewDialog.title" @close="previewDialog.show = false">
    <div class="preview-content-centered">
      <template v-if="previewDialog.imageData.includes('application/pdf')">
        <iframe :src="previewDialog.imageData" class="pdf-preview-frame" width="100%" height="500px"></iframe>
      </template>
      <template v-else>
        <img :src="previewDialog.imageData" class="full-preview-img" />
      </template>

      <div class="preview-footer">
        <button @click="previewDialog.show = false" class="close-preview-btn">Close</button>
      </div>
    </div>
  </DialogBox>

  <DialogBox :show="confirmDialog.show" :title="confirmDialog.title" :message="confirmDialog.message" @close="cancelClear">
    <div class="confirm-actions">
      <button @click.stop="cancelClear($event)" class="btn-secondary">No, Keep Data</button>
      <button @click="confirmClearVehicles" class="btn-danger">Yes, Remove All</button>
    </div>
  </DialogBox>

  <DialogBox :show="successDialog.show"
             :title="successDialog.title"
             :message="successDialog.message"
             @close="handleSuccessClose">
    <div class="confirm-actions">
      <button @click="handleSuccessClose" class="proceed-btn">Return to Homepage</button>
    </div>
  </DialogBox>
  <DialogBox :show="errorDialog.show"
             :title="errorDialog.title"
             :message="errorDialog.message"
             @close="errorDialog.show = false">
    <div class="confirm-actions">
      <button @click="errorDialog.show = false" class="proceed-btn">
        I Understand
      </button>
    </div>
  </DialogBox>
</template>

<script setup>
  import { computed, nextTick, onMounted, ref, watch } from 'vue';
  import axios from 'axios';
  import DialogBox from '@/components/DialogBox.vue';

  import bgImage from '@/assets/NPO_PSSIC_background.jpeg';

  import { marked } from 'marked';

  const currentStep = ref('privacy');
  const isLoading = ref(false);
  const submittedOnce = ref(false); // Flag to track submission attempts

  const form = ref({
    branchId: null,
    visitDate: '',
    visitTime: '',
    purposeOfVisit: '',
    authorizedPersonnel: '', // This maps to PersonToVisit in backend
    department: '',
    agreedToPrivacy: false,
    needsCarpass: false,

    // REGISTRANT (Organizer)
    registrant: {
      prefix: '',
      firstName: '',
      middleName: '',
      lastName: '',
      suffix: '',
      companyName: '', // This maps to OrgName in backend
      designation: '',
      email: '',
      validId: '',
      fileName: '',
      isAlsoVisitor: false // If true, we duplicate this into the visitors array for the backend
    },

    visitors: [], // Additional visitors (not including the registrant)
    vehicles: []
  });

  const errors = ref({
    branchId: '',
    authorizedPersonnel: '',
    purposeOfVisit: '',
    visitDate: '',
    visitTime: '',
    registrant: {}, // New error nesting
    visitors: [],
    vehicles: []
  });

  const privacyHtml = ref('');

  const validateForm = () => {
    let isValid = true;
    const now = new Date();

    // Format current date to YYYY-MM-DD for comparison
    const todayStr = now.toISOString().split('T')[0];

    errors.value = {
      branchId: !form.value.branchId ? 'Branch is required' : '',
      companyName: !form.value.companyName ? 'Company/Organization is required' : '',
      authorizedPersonnel: !form.value.authorizedPersonnel ? 'Person to visit is required' : '',
      department: !form.value.department ? 'Department is required' : '',
      purposeOfVisit: !form.value.purposeOfVisit ? 'Purpose of visit is required' : '',
      visitDate: '',
      visitTime: '',
      registrant: {},
      visitors: [],
      vehicles: []
    };

    // 1. Date Validation (Past Date Check)
    if (!form.value.visitDate) {
      errors.value.visitDate = 'Date is required';
    } else {
      const selectedDateObj = new Date(form.value.visitDate);
      const dayOfWeek = selectedDateObj.getDay(); // 0 = Sunday, 1 = Monday...

      if (form.value.visitDate < todayStr) {
        errors.value.visitDate = 'Cannot schedule for a past date';
      } else if (dayOfWeek === 0) {
        errors.value.visitDate = 'Office is closed on Sundays. Please select another date.';
      }
    }

    // 2. Time Validation (Past Time & Office Hours Check)
    if (!form.value.visitTime) {
      errors.value.visitTime = "Time is required";
      isValid = false;
    } else {
      // Combine current Date and Time into a single object for comparison
      const selectedDateTime = new Date(`${form.value.visitDate}T${form.value.visitTime}`);
      const now = new Date();

      // If the date is Today, check if the time is in the past
      if (form.value.visitDate === todayStr && selectedDateTime < now) {
        errors.value.visitTime = "Selected time has already passed";
        isValid = false;
      }
    }

    // Check top-level errors
    if (errors.value.branchId || errors.value.visitDate || errors.value.visitTime ||
      errors.value.authorizedPersonnel || errors.value.purposeOfVisit || errors.value.department) {
      isValid = false;
    }

    // Validate Registrant
    const r = form.value.registrant;
    const rErrors = {
      firstName: !r.firstName ? 'Required' : '',
      lastName: !r.lastName ? 'Required' : '',
      companyName: !r.companyName ? 'Required' : '', // New Validation
      designation: !r.designation ? 'Required' : '',
      email: !r.email ? 'Required' : (!/\S+@\S+\.\S+/.test(r.email) ? 'Invalid email' : ''),
      validId: !r.validId ? 'Identity verification required' : ''
    };
    errors.value.registrant = rErrors;
    if (Object.values(rErrors).some(err => err !== '')) isValid = false;

    // Validate Additional Visitors
    form.value.visitors.forEach((v, index) => {
      const vErrors = {
        firstName: !v.firstName ? 'Required' : '',
        lastName: !v.lastName ? 'Required' : '',
        companyName: !v.companyName ? 'Required' : '', // New Validation
        designation: !v.designation ? 'Required' : '',
        email: !v.email ? 'Required' : '',
        validId: !v.validId ? 'ID photo required' : ''
      };
      errors.value.visitors[index] = vErrors;
      if (Object.values(vErrors).some(err => err !== '')) isValid = false;
    });

    // Validate Vehicles
    form.value.vehicles.forEach((c, index) => {
      const cErrors = {
        driverFirstName: !c.driverFirstName ? 'Required' : '',
        driverLastName: !c.driverLastName ? 'Required' : '',
        plateNumber: !c.plateNumber ? 'Required' : '',
        model: !c.model ? 'Required' : '', // ADDED
        color: !c.color ? 'Required' : '', // ADDED
        validId: !c.validId ? 'License photo required' : ''
      };
      errors.value.vehicles[index] = cErrors;
      if (Object.values(cErrors).some(err => err !== '')) isValid = false;
    });

    if (form.value.needsCarpass) {
      form.value.vehicles.forEach((c, index) => {
        const cErrors = {
          plateNumber: !c.plateNumber ? 'Required' : '',
          model: !c.model ? 'Required' : '', // ADDED
          color: !c.color ? 'Required' : '', // ADDED
          validId: !c.validId ? 'License photo required' : ''
        };

        // Only require driver names if driver is NOT a visitor (synced)
        if (!c.driverIsVisitor) {
          if (!c.driverFirstName) cErrors.driverFirstName = 'Required';
          if (!c.driverLastName) cErrors.driverLastName = 'Required';
        } else {
          // If synced, ensure a visitor was actually selected
          if (c.selectedVisitorIndex === null || c.selectedVisitorIndex === undefined) {
            cErrors.selectedVisitorIndex = 'Please select a driver';
          }
        }

        errors.value.vehicles[index] = cErrors;
        if (Object.values(cErrors).some(err => err !== '')) isValid = false;
      });
    }

    return isValid;
  };

  // Real-time error clearing
  const clearError = (section, field) => {
    if (field && errors.value[section]) {
      errors.value[section][field] = '';
    } else if (errors.value[section]) {
      errors.value[section] = '';
    }
  };

  const clearNestedError = (collection, index, fieldName) => {
    if (errors.value[collection] && errors.value[collection][index]) {
      errors.value[collection][index][fieldName] = '';
    }
  };

  const hasErrors = computed(() => {
    if (!submittedOnce.value || successDialog.value.show || isLoading.value) return false;

    const hasMainErrors = Object.values(errors.value).some(err => typeof err === 'string' && err !== '');
    const hasRegistrantErrors = Object.values(errors.value.registrant || {}).some(val => val !== '');

    // Check if any visitor or vehicle error objects have actual error messages
    const hasVisitorErrors = errors.value.visitors.some(v => v && Object.values(v).some(val => val !== ''));
    const hasVehicleErrors = errors.value.vehicles.some(c => c && Object.values(c).some(val => val !== ''));

    return hasMainErrors || hasRegistrantErrors || hasVisitorErrors || hasVehicleErrors;
  });

  const resetErrors = () => {
    errors.value = {
      branchId: '',
      companyName: '',
      authorizedPersonnel: '',
      purposeOfVisit: '',
      visitDate: '',
      visitTime: '',
      registrant: {},
      visitors: [],
      vehicles: []
    };
  };

  const proceedToForm = () => {
    if (form.value.agreedToPrivacy) {
      currentStep.value = 'form';
      nextTick(() => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
      });
    }
  };

  onMounted(async () => {
    try {
      const response = await fetch('/privacy-notice.md');
      const text = await response.text();
      // Convert the text to HTML and save it to our ref
      privacyHtml.value = marked(text);
    } catch (err) {
      console.error("Failed to load privacy text:", err);
      privacyHtml.value = "<p>Error loading privacy notice. Please contact administration.</p>";
    }

    // Clear the saved agreement so a refresh always starts at 'privacy'
    localStorage.removeItem('hasAgreedToPrivacy');

    // Explicitly set the start state
    form.value.agreedToPrivacy = false;
    currentStep.value = 'privacy';

    // Build the initial valid ISO string immediately
    const today = new Date().toISOString().split('T')[0];
    form.value.visitDate = today;
    form.value.visitTime = "08:00:00";
    form.value.visitDateTime = `${today}T08:00:00`;
  });

  const timeParts = ref({
    hour: 8,
    minute: '00',
    period: 'AM'
  });

  // Example logic for splitting a "14:30:00" string back into the 3 dropdowns
  const loadExistingTime = (timeString) => {
    if (!timeString) return;

    const [h24, min] = timeString.split(':');
    let h = parseInt(h24);
    const period = h >= 12 ? 'PM' : 'AM';

    if (h > 12) h -= 12;
    if (h === 0) h = 12;

    timeParts.value = {
      hour: h,
      minute: min,
      period: period
    };
  };

  watch([timeParts, () => form.value.visitDate], () => {
    let h = parseInt(timeParts.value.hour);
    if (timeParts.value.period === 'PM' && h < 12) h += 12;
    if (timeParts.value.period === 'AM' && h === 12) h = 0;

    const formattedHour = h.toString().padStart(2, '0');
    const timeString = `${formattedHour}:${timeParts.value.minute}:00`;
    const datePart = form.value.visitDate || new Date().toISOString().split('T')[0];

    form.value.visitTime = timeString; // "HH:mm:ss"
    form.value.visitDateTime = `${datePart}T${timeString}`; // "YYYY-MM-DDTHH:mm:ss"

    if (errors.value) {
      errors.value.visitTime = '';
    }
  }, { deep: true, immediate: true });

  const addVisitor = () => {
    form.value.visitors.push({
      prefix: '',
      firstName: '',
      middleName: '',
      lastName: '',
      suffix: '',
      companyName: '',
      designation: '',
      email: '',
      validId: '',
      fileName: ''
    });
  };

  const removeVisitor = (index) => {
    // Removed the length > 1 check so even the first companion can be deleted
    form.value.visitors.splice(index, 1);
  };

  const handleDriverToggle = (car, index) => {
    car.selectedVisitorIndex = null;
    if (!car.driverIsVisitor) {
      car.driverFirstName = ''; car.driverMiddleName = ''; car.driverLastName = ''; car.validId = '';
    }
    if (errors.value.vehicles[index]) {
      errors.value.vehicles[index] = {}; // Clear error object for this vehicle on toggle
    }
  };

  const toggleVehicle = () => {
    if (!form.value.needsCarpass && form.value.vehicles.length > 0) {
      // Force the checkbox to stay checked until they confirm
      form.value.needsCarpass = true;

      // Populate the dialog content
      confirmDialog.value = {
        show: true,
        title: 'Remove Vehicle Information?',
        message: 'You have already entered vehicle/driver details. Are you sure you want to clear all this data and remove the carpass request?'
      };
    } else if (form.value.needsCarpass && form.value.vehicles.length === 0) {
      addVehicle();
    }
  };

  const confirmClearVehicles = () => {
    form.value.vehicles = [];
    form.value.needsCarpass = false;
    confirmDialog.value.show = false;
  };

  const addVehicle = () => {
    form.value.vehicles.push({
      driverIsVisitor: false, selectedVisitorIndex: null,
      driverFirstName: '', driverMiddleName: '', driverLastName: '',
      validId: '', plateNumber: '', model: '', color: '', fileName: ''
    });
  };

  const removeVehicle = (index) => {
    form.value.vehicles.splice(index, 1);
    if (form.value.vehicles.length === 0) form.value.needsCarpass = false;
  };

  const visitingPersonnel = computed(() => {
    const list = [];

    // Add registrant if they are marked as a visitor
    if (form.value.registrant.isAlsoVisitor) {
      list.push({
        fullName: `${form.value.registrant.firstName} ${form.value.registrant.lastName} (Registrant)`,
        // We use a unique ID or index. 
        // Using a string like 'registrant' helps distinguish it from array indices.
        id: 'registrant',
        data: form.value.registrant
      });
    }

    // Add all other companions
    form.value.visitors.forEach((v, index) => {
      list.push({
        fullName: `${v.firstName} ${v.lastName}`,
        id: index,
        data: v
      });
    });

    return list;
  });

  const syncDriver = (selection, carIdx) => {
    const car = form.value.vehicles[carIdx];
    let sourceData = null;

    if (selection === 'registrant') {
      sourceData = form.value.registrant;
    } else if (typeof selection === 'number') {
      sourceData = form.value.visitors[selection];
    }

    if (sourceData) {
      car.driverFirstName = sourceData.firstName;
      car.driverLastName = sourceData.lastName;
      car.driverMiddleName = sourceData.middleName;
      car.validId = sourceData.validId; // Sync the image
    } else {
      // Reset if null
      car.driverFirstName = '';
      car.driverLastName = '';
      car.validId = '';
    }
  };

  watch(() => form.value.registrant.isAlsoVisitor, (newVal) => {
    if (!newVal) {
      form.value.vehicles.forEach((car, index) => {
        if (car.selectedVisitorIndex === 'registrant') {
          car.selectedVisitorIndex = null;
          syncDriver(null, index);
        }
      });
    }
  });

  const triggerUpload = (id) => {
    const el = document.getElementById(id);
    if (el) el.click();
  };

  // Add this to your refs section in <script setup>
  const errorDialog = ref({
    show: false,
    title: 'File Too Large',
    message: ''
  });

  const handleFileUpload = (event, targetObj) => {
    const file = event.target.files[0];
    if (!file) return;

    const MAX_SIZE_MB = 5;
    const MAX_SIZE_BYTES = MAX_SIZE_MB * 1024 * 1024;

    if (file.size > MAX_SIZE_BYTES) {
      const actualSize = (file.size / (1024 * 1024)).toFixed(2);
      errorDialog.value = {
        show: true,
        title: 'Upload Error',
        message: `The file you selected is ${actualSize}MB. Please select a file smaller than ${MAX_SIZE_MB}MB.`
      };
      event.target.value = '';
      return;
    }

    targetObj.fileName = file.name;
    const reader = new FileReader();

    reader.onload = (e) => {
      // PDF Handling: Skip compression
      if (file.type === 'application/pdf') {
        targetObj.validId = e.target.result;
        console.log("PDF loaded as Base64");
      }
      // Image Handling: Run compression
      else if (file.type.startsWith('image/')) {
        const img = new Image();
        img.src = e.target.result;
        img.onload = () => {
          targetObj.validId = compressImage(img, 1200, 0.7);
        };
      }
    };

    reader.readAsDataURL(file);
  };

  const compressImage = (img, maxDimension, initialQuality) => {
    const canvas = document.createElement('canvas');
    let width = img.width;
    let height = img.height;

    // Maintain Aspect Ratio
    if (width > height) {
      if (width > maxDimension) { height *= maxDimension / width; width = maxDimension; }
    } else {
      if (height > maxDimension) { width *= maxDimension / height; height = maxDimension; }
    }

    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext('2d');
    ctx.drawImage(img, 0, 0, width, height);

    let quality = initialQuality;
    let dataUrl = canvas.toDataURL('image/jpeg', quality);

    // 3. Logic to force size below 200KB
    // We loop, reducing quality by 0.1 until size < 200KB or quality is too low
    let sizeKB = (dataUrl.length * (3 / 4) / 1024); // Accurate Base64 to KB estimation

    while (sizeKB > 200 && quality > 0.1) {
      quality -= 0.1;
      dataUrl = canvas.toDataURL('image/jpeg', quality);
      sizeKB = (dataUrl.length * (3 / 4) / 1024);
    }

    console.log(`Compressed Size: ${sizeKB.toFixed(2)} KB (Quality: ${quality.toFixed(1)})`);
    console.log('---------------------------');

    return dataUrl;
  };

  const successDialog = ref({ show: false, title: 'Registration Successful', message: '' });
  const previewDialog = ref({ show: false, title: '', imageData: '' });
  const confirmDialog = ref({ show: false, title: '', message: '' });
  const isTakingLong = ref(false);

  const openFullImage = (base64Data, title) => {
    previewDialog.value = { show: true, title, imageData: base64Data };
  };

  const cancelClear = () => { confirmDialog.value.show = false; };

  const removeFile = (targetObj, type = 'registrant', index = null) => {
    // 1. Clear the data
    targetObj.validId = '';
    targetObj.fileName = '';

    // 2. Clear the error so the red border vanishes when they delete the "bad" file
    if (type === 'registrant') {
      errors.value.registrant.validId = '';
    } else if (type === 'visitors' && index !== null) {
      if (errors.value.visitors[index]) {
        errors.value.visitors[index].validId = '';
      }
    } else if (type === 'vehicles' && index !== null) {
      if (errors.value.vehicles[index]) {
        errors.value.vehicles[index].validId = '';
      }
    }
  };

  const handleSubmit = async () => {
    submittedOnce.value = true;

    if (!form.value.agreedToPrivacy) return alert("Please agree to the Data Privacy Notice.");

    // validateForm() updates the 'errors' object
    if (!validateForm()) {
      return window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    const hasAtLeastOneVisitor = form.value.registrant.isAlsoVisitor || form.value.visitors.length > 0;

    if (!hasAtLeastOneVisitor) {
      errorDialog.value = {
        show: true,
        title: 'No Visitors Selected',
        message: 'Please either check "I am also a visitor" or add at least one companion to proceed.'
      };
      return;
    }

    isLoading.value = true;
    try {
      const payload = {
        // Note: Backend now uses 'registrantOrganization' but we map it here
        registrantOrganization: form.value.registrant.companyName,
        visitDateTime: `${form.value.visitDate}T${form.value.visitTime}`,
        branchId: form.value.branchId,
        personToVisit: form.value.authorizedPersonnel,
        purposeOfVisit: form.value.purposeOfVisit,
        Department_PersonToVisit: form.value.department,

        registrantPrefix: form.value.registrant.prefix || "",
        registrantFirstName: form.value.registrant.firstName,
        registrantMiddleName: form.value.registrant.middleName || "",
        registrantLastName: form.value.registrant.lastName,
        registrantSuffix: form.value.registrant.suffix || "",
        registrantEmail: form.value.registrant.email,
        registrantDesignation: form.value.registrant.designation,
        registrantValidID: cleanBase64(form.value.registrant.validId),

        visitors: [],
        vehicles: []
      };

      if (form.value.registrant.isAlsoVisitor) {
        payload.visitors.push(mapVisitorToDTO(form.value.registrant));
      }

      form.value.visitors.forEach(v => {
        payload.visitors.push(mapVisitorToDTO(v));
      });

      payload.vehicles = form.value.vehicles.map(v => ({
        firstName: v.driverFirstName,
        lastName: v.driverLastName,
        middleName: v.driverMiddleName || "",
        validIdPath: cleanBase64(v.validId),
        plateNo: v.plateNumber,
        model: v.model,
        color: v.color
      }));

      const response = await axios.post('/api/VisitorMonitoring/schedule-visit', payload);

      if (response.status === 200) {
        // Hides the red error summary box immediately
        submittedOnce.value = false;

        resetErrors();

        successDialog.value = {
          show: true,
          title: 'Success',
          message: response.data.message
        };
      }
    } catch (error) {
      // Keep submittedOnce = true so the errors stay visible if the API failed
      alert("Error: " + (error.response?.data?.error || "Submission failed."));
    } finally {
      isLoading.value = false;
    }
  };

  const handleSuccessClose = () => {
    successDialog.value.show = false;
    localStorage.removeItem('hasAgreedToPrivacy');
    resetForm();
    currentStep.value = 'privacy';
    window.scrollTo(0, 0);
  };

  const cleanBase64 = (str) => (str && str.includes(',') ? str.split(',')[1] : (str || ""));

  const mapVisitorToDTO = (v) => ({
    prefix: v.prefix,
    firstName: v.firstName,
    middleName: v.middleName || "",
    lastName: v.lastName,
    suffix: v.suffix,
    organization: v.companyName,
    email: v.email,
    designation: v.designation,
    //department: "",
    validId: cleanBase64(v.validId)
  });

  const resetForm = () => {
    submittedOnce.value = false;
    form.value = {
      branchId: null, visitDate: '', visitTime: '',
      purposeOfVisit: '', authorizedPersonnel: '', agreedToPrivacy: false, needsCarpass: false,
      registrant: {
        prefix: '', firstName: '', middleName: '', lastName: '', suffix: '',
        companyName: '', designation: '', email: '', validId: '', fileName: '', isAlsoVisitor: false
      },
      visitors: [],
      vehicles: []
    };
    errors.value = { branchId: '', authorizedPersonnel: '', purposeOfVisit: '', visitDate: '', visitTime: '', registrant: {}, visitors: [], vehicles: [] };
  };
</script>

<style scoped>
  * {
    box-sizing: border-box;
  }

  @import url('https://fonts.googleapis.com/css2?family=Montserrat:wght@300;400;600;800&display=swap');

  .registration-wrapper {
    font-family: 'Montserrat', sans-serif;
    min-height: 100vh; /* Changed from min-height to height */
    width: 100%;
    display: flex;
    justify-content: center;
    align-items: flex-start; /* Center the box perfectly */
    background: linear-gradient( rgba(244, 247, 249, 0.8), rgba(244, 247, 249, 0.8) ), v-bind('"url(" + bgImage + ")"');
    background-size: cover;
    background-position: center;
    padding: 20px 10px;
    overflow-x: hidden; /* Prevent the main page from scrolling */
  }

  .registration-container {
    display: flex;
    flex-direction: column;
    width: 100%;
    max-width: 100%;
    height: 100%; /* Fill the 100vh of the wrapper minus padding */
    max-height: 90vh; /* Safeguard for small screens */
    gap: 15px;
    margin: 0;
  }

  .header-content {
    display: flex;
    flex-direction: column;
    align-items: flex-start; /* Keeps everything aligned to the left */
    gap: 4px; /* Natural spacing between lines */
  }

  .form-card {
    width: 100%;
    flex: 1; /* This makes the card fill the remaining height of the container */
    display: flex;
    flex-direction: column;
    padding: 40px;
    border-radius: 20px;
    box-shadow: 0 20px 40px rgba(0,0,0,0.15);
    backdrop-filter: blur(10px);
    background: rgba(255, 255, 255, 0.9);
    overflow: hidden; /* Don't let the card itself scroll */
  }

  /* Adjust padding for desktop only */
  @media (min-width: 1024px) {
    .form-card {
      padding: 40px;
    }
  }

  /* Optional: Add a little extra space for the header so it doesn't feel cramped */
  .form-header-action {
    width: 100%;
    display: flex;
    justify-content: flex-start;
    margin-bottom: 10px;
  }

  /* Header Container */
  .form-main-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end; /* Aligns button with the bottom of the text */
    margin-bottom: 5px;
    padding: 0 10px;
  }

  .form-title {
    color: #003366;
    font-size: 1.8rem;
    font-weight: 800;
    margin: 0;
    text-transform: uppercase;
    letter-spacing: -0.5px;
  }

  .form-subtitle {
    color: #4a5568;
    font-size: 0.95rem;
    margin: 5px 0 0 0;
    font-weight: 400;
  }

  form {
    flex: 1;
    overflow-y: auto; /* Only the inputs scroll */
    padding-right: 15px; /* Space for the scrollbar */
    margin-right: -5px;
  }

    /* Custom Scrollbar for a cleaner look */
    form::-webkit-scrollbar {
      width: 8px;
    }

    form::-webkit-scrollbar-track {
      background: #f1f1f1;
      border-radius: 10px;
    }

    form::-webkit-scrollbar-thumb {
      background: #003366;
      border-radius: 10px;
    }

  .registration-container {
    display: flex; /* Changed from grid */
    flex-direction: column;
    width: 100%;
    max-width: 1400px; /* Increased from 1200px to give more room for the repeater cards */
    gap: 20px;
    margin: 0 auto; /* Ensures it stays centered */
  }

  .privacy-container {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 90dvh;
    padding: 20px;
  }

  .dark-text {
    color: #1a202c !important; /* Deep charcoal/black */
  }

  .privacy-card {
    width: 100%;
    max-width: 700px;
    background: white;
    /* FIX: Reduce padding for mobile */
    padding: 2rem;
    border-radius: 12px;
    display: flex;
    flex-direction: column;
    max-height: 85dvh; /* Keep the whole card visible */
  }

  .privacy-content {
    background: #f1f5f9;
    padding: 15px;
    border-radius: 8px;
    /* FIX: Use a relative max-height so it shrinks on small phones */
    max-height: 50dvh;
    overflow-y: auto;
    -webkit-overflow-scrolling: touch;
    margin: 1rem 0;
    border: 1px solid #e2e8f0;
    white-space: pre-wrap;
    color: #333333; /* A nice dark grey, or use #000 for pure black */
    line-height: 1.6;
    font-family: 'Montserrat', sans-serif; /* Keep it consistent with your font */
    text-align: justify; /* Optional: makes it look more like a legal document */
  }

    /* If you want the specific "Data Privacy Act" text or emails to be darker/bold */
    .privacy-content b,
    .privacy-content strong {
      color: #000000;
      font-weight: 600;
    }

    .privacy-content::-webkit-scrollbar {
      width: 6px;
    }

    .privacy-content::-webkit-scrollbar-thumb {
      background: #cbd5e0;
      border-radius: 10px;
    }

  .checkbox-wrapper label {
    color: #1a202c !important;
    font-weight: 600;
  }

  /* Make the main title and subheadings dark */
  .privacy-card h1,
  .privacy-card h3 {
    color: #003366 !important;
  }

  /* Make sure the text isn't too light on mobile */
  @media (max-width: 768px) {
    .privacy-content {
      font-size: 14px;
      color: #222222;
    }
  }

  .proceed-btn {
    width: 100%;
    background: #003366;
    color: white;
    padding: 15px;
    border-radius: 8px;
    font-weight: bold;
    cursor: pointer;
    border: none;
    transition: background 0.3s;
  }

    .proceed-btn:disabled {
      background: #cbd5e0;
      cursor: not-allowed;
    }

  .back-link {
    background: none;
    border: none;
    color: #003366;
    cursor: pointer;
    font-weight: 600;
    margin-bottom: 10px;
  }

  .back-icon {
    font-size: 1.1rem;
  }

  .privacy-box, .form-card {
    backdrop-filter: blur(10px);
    background: rgba(255, 255, 255, 0.9);
    border: 1px solid rgba(255, 255, 255, 0.3);
  }

  .info-side {
    color: #1a202c;
  }

  .sticky-content {
    position: sticky;
    top: 40px;
  }

  .main-title {
    color: #003366;
    font-size: 2.4rem;
    font-weight: 800;
    margin-bottom: 20px;
    line-height: 1.1;
    letter-spacing: -0.5px;
  }

  .intro-text {
    font-size: 1.1rem;
    line-height: 1.6;
    margin-bottom: 30px;
    color: #2d3748;
  }

  .privacy-box {
    background: rgba(255, 255, 255, 0.85);
    padding: 30px;
    border-radius: 16px;
    border-left: 6px solid #003366;
    box-shadow: 0 10px 25px rgba(0,0,0,0.1);
  }

    .privacy-box h3 {
      margin-top: 0;
      color: #003366;
      font-size: 1.3rem;
      font-weight: 700;
      margin-bottom: 15px;
    }

    .privacy-box p, .privacy-box li {
      font-size: 0.95rem;
      line-height: 1.5;
      color: #4a5568;
    }

    .privacy-box ul {
      padding-left: 20px;
      margin-bottom: 20px;
    }

  .consent-text {
    font-style: italic;
    font-size: 0.85rem;
    color: #718096;
    border-top: 1px solid #cbd5e0;
    padding-top: 15px;
  }

  .form-header-action {
    width: 100%;
    display: flex;
    justify-content: flex-start;
  }

  .section-label {
    display: block;
    font-weight: 800;
    font-size: 1.1rem;
    text-transform: uppercase;
    letter-spacing: 1px;
    margin-bottom: 25px;
    color: #003366;
    border-bottom: 2px solid #003366;
    padding-bottom: 8px;
  }

  .logistics-grid {
    display: grid;
    grid-template-columns: 1fr 1fr; /* Two equal columns */
    gap: 30px;
    align-items: start;
  }

  .logistics-column {
    display: flex;
    flex-direction: column;
  }

  /* Adjust textarea to feel more balanced next to inputs */
  textarea {
    resize: none;
    min-height: 100px;
  }

  .registrant-card {
    border-left: 4px solid #0056b3; /* Give it a distinct "Primary" look */
    background-color: #f8f9fa;
  }

  .section-divider {
    margin: 2rem 0;
    border: 0;
    border-top: 1px dashed #ccc;
  }

  /* 1. Ensure the container has enough room and doesn't squeeze columns */
  .time-picker-grid {
    display: grid;
    grid-template-columns: repeat(3, minmax(100px, 1fr)); /* Force a minimum width per column */
    gap: 12px;
    width: 100%;
    max-width: 450px;
  }

    /* 2. Fix the overlap inside the selects */
    .time-picker-grid .main-select {
      padding: 12px 30px 12px 12px; /* Extra right padding (30px) to clear the arrow */
      text-align: left; /* Alignment change helps with overlap */
      appearance: none;
      background-image: url("data:image/svg+xml;charset=UTF-8,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24' fill='none' stroke='%23003366' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'%3e%3cpolyline points='6 9 12 15 18 9'%3e%3c/polyline%3e%3c/svg%3e");
      background-repeat: no-repeat;
      background-position: right 10px center; /* Positions arrow safely away from the text */
      background-size: 14px;
      cursor: pointer;
      width: 100%;
    }

  /* 3. Responsive fix for very small phones */
  @media (max-width: 400px) {
    .time-picker-grid {
      grid-template-columns: 1fr; /* Stack them vertically if the screen is too narrow */
      gap: 8px;
    }

      .time-picker-grid .main-select {
        max-width: 100%;
      }
  }

  .input-field {
    margin-bottom: 20px;
    display: flex;
    flex-direction: column;
  }

    .input-field label {
      font-weight: 600;
      font-size: 0.85rem;
      margin-bottom: 8px;
      color: #4a5568;
    }

  input, textarea {
    font-family: 'Montserrat', sans-serif;
    padding: 14px;
    border: 1px solid #cbd5e0;
    border-radius: 10px;
    font-size: 1rem;
    background: white;
    transition: all 0.2s;
  }

    input:focus, textarea:focus {
      outline: none;
      border-color: #003366;
      box-shadow: 0 0 0 3px rgba(0, 51, 102, 0.1);
    }

  .input-group-row {
    display: flex;
    gap: 15px;
  }

  /* Name Grid Layout */
  .input-grid-names {
    display: grid;
    grid-template-columns: 0.6fr 1.5fr 1fr 1.5fr 0.6fr; /* Responsive proportions */
    gap: 15px;
    margin-bottom: 20px;
  }

  /* Specific styling for Registrant Checkbox area */
  /*.registrant-options {
    background: #f1f5f9;
    padding: 12px 15px;
    border-radius: 6px;
    margin-bottom: 20px;
    border-left: 4px solid #3b82f6;
  }*/

  /* Mobile Responsiveness for the Name Grid */
  @media (max-width: 900px) {
    .input-grid-names {
      grid-template-columns: 1fr 1fr; /* Stacks nicely on smaller screens */
    }

    .col-prefix, .col-suffix {
      grid-column: span 1;
    }
  }

  .upload-control-group {
    display: flex;
    flex-direction: column; /* Stack preview and button */
    align-items: flex-start;
    width: fit-content; /* KEY: Makes the border wrap the button width */
    padding: 2px;
    border: 2px solid transparent;
    border-radius: 8px;
    transition: border-color 0.3s ease;
  }

  .upload-buttons {
    display: flex;
    gap: 8px;
    align-items: center;
    justify-content: flex-start; /* Ensure they stay on the left */
    width: 100%;
  }

  .upload-btn {
    background: #edf2f7;
    border: 1px dashed #003366;
    color: #003366;
    padding: 10px 20px; /* Added horizontal padding for better shape */
    border-radius: 8px;
    cursor: pointer;
    font-weight: 600;
    font-size: 0.85rem;
    /* CHANGE THESE LINES */
    flex: 0 1 auto; /* Stop it from growing to fill space */
    width: fit-content; /* Only take up as much space as the text needs */
    min-width: 140px; /* Gives a consistent base size */
    /* ------------------ */

    transition: all 0.2s;
    display: block;
  }

    .upload-btn:hover {
      background: #e2e8f0;
      border-style: solid; /* Optional: makes it feel more "active" on hover */
    }

  .remove-id-btn {
    background: #fee2e2;
    color: #dc2626;
    border: none;
    width: 35px;
    height: 35px;
    border-radius: 8px;
    cursor: pointer;
    display: flex;
    justify-content: center;
    align-items: center;
    font-weight: bold;
  }

  .file-name-label {
    font-size: 0.75rem;
    color: #2f855a;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 200px;
  }

  .id-preview-container {
    margin-bottom: 10px;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
  }

  .id-thumbnail {
    width: 80px;
    height: 50px;
    object-fit: cover;
    border-radius: 6px;
    border: 2px solid #003366;
    cursor: zoom-in;
    transition: transform 0.2s;
  }

    .id-thumbnail:hover {
      transform: scale(1.05);
    }

  .preview-hint {
    font-size: 0.65rem;
    color: #718096;
    margin: 2px 0 0 0;
  }

  .preview-content-centered {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 20px;
  }

  .full-preview-img {
    max-width: 100%;
    max-height: 70vh; /* Limits height so it doesn't push the button off screen */
    object-fit: contain;
    border-radius: 8px;
    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
  }

  .preview-footer {
    width: 100%;
    display: flex;
    justify-content: flex-end;
  }

  .close-preview-btn {
    padding: 10px 20px;
    background: #002a57;
    color: white;
    border: none;
    border-radius: 4px;
    font-weight: 600;
    cursor: pointer;
  }

    .close-preview-btn:hover {
      background: #004085;
    }

  .preview-wrapper {
    position: relative;
    display: inline-block;
  }

  .pdf-preview-frame {
    border: none;
    border-radius: 8px;
    box-shadow: 0 2px 10px rgba(0,0,0,0.1);
  }

  /* Optional: change the thumbnail look if it's a PDF */
  .id-thumbnail {
    object-fit: cover;
    cursor: pointer;
    background-color: #f8f9fa; /* Help see PDF icons or text clearly */
  }

  .remove-file-btn {
    position: absolute;
    top: -8px;
    right: -8px;
    background: #dc3545; /* Danger Red */
    color: white;
    border: 2px solid white;
    border-radius: 50%;
    width: 24px;
    height: 24px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    cursor: pointer;
    box-shadow: 0 2px 4px rgba(0,0,0,0.2);
    z-index: 10;
    transition: transform 0.2s;
  }

    .remove-file-btn:hover {
      transform: scale(1.1);
      background: #a71d2a;
    }

  /* ADD THIS TO YOUR STYLE BLOCK */

  .synced-preview {
    margin-top: 15px; /* Matches the spacing used in .border-top */
    padding: 12px;
    background-color: #f8fafc; /* Very light professional grey */
    border-radius: 10px;
    border: 1px dashed #cbd5e1;
    display: flex;
    flex-direction: column;
    align-items: flex-start;
  }

  .mini-label {
    display: block;
    font-size: 0.7rem;
    color: #64748b;
    margin-bottom: 8px;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 0.5px;
  }

  /* Ensure the thumbnail within the synced container looks correct */
  .synced-preview .id-thumbnail {
    margin-bottom: 0; /* Override the default container margin */
  }

  .checkbox-wrapper {
    display: flex;
    align-items: center;
    gap: 12px;
    margin: 25px 0;
    font-weight: 600;
    color: #2d3748;
    cursor: pointer;
  }

  .confirm-actions {
    display: flex;
    justify-content: flex-end;
    gap: 12px;
    margin-top: 20px;
  }

  .btn-secondary {
    padding: 10px 20px;
    background: #e5e7eb;
    color: #374151;
    border: none;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

  .btn-danger {
    padding: 10px 20px;
    background: #dc2626; /* Warning Red */
    color: white;
    border: none;
    border-radius: 6px;
    font-weight: 600;
    cursor: pointer;
  }

    .btn-danger:hover {
      background: #b91c1c;
    }

  .btn-secondary:hover {
    background: #d1d5db;
  }

  .input-error {
    border: 2px solid #dc3545 !important;
    background-color: #fff8f8;
  }

  .error-text {
    color: #dc3545;
    font-size: 0.75rem;
    margin-top: 4px;
    font-weight: 500;
  }

  .error-summary-box {
    background: #fdecea;
    border-left: 5px solid #dc3545;
    padding: 15px;
    margin-bottom: 20px;
    border-radius: 4px;
  }

  .error-summary-box {
    background-color: #fee2e2; /* Light red background */
    border: 1px solid #ef4444; /* Strong red border */
    border-radius: 8px;
    padding: 12px 16px;
    margin-bottom: 20px;
  }

    .error-summary-box p {
      color: #b91c1c !important; /* Deep red for high contrast */
      font-weight: 600;
      margin: 0;
      display: flex;
      align-items: center;
      gap: 8px;
    }

  .upload-error-border {
    border: 2px dashed #dc2626 !important;
    background-color: #fff5f5; /* Light red tint to make it obvious */
  }

  .wait-warning {
    color: #856404;
    background: #fff3cd;
    padding: 10px;
    border-radius: 4px;
    margin-top: 10px;
    text-align: center;
    font-size: 0.9rem;
    border: 1px solid #ffeeba;
  }

  .submit-btn {
    font-family: 'Montserrat', sans-serif;
    width: 100%;
    padding: 18px;
    background: #003366;
    color: white;
    border: none;
    border-radius: 10px;
    font-size: 1.1rem;
    font-weight: 700;
    text-transform: uppercase;
    letter-spacing: 1px;
    cursor: pointer;
    margin-top: 20px;
    transition: all 0.3s ease;
  }

    .submit-btn:hover:not(:disabled) {
      background: #002244;
      transform: translateY(-2px);
      box-shadow: 0 5px 15px rgba(0, 51, 102, 0.3);
    }

    .submit-btn:disabled {
      background: #a0aec0;
      cursor: not-allowed;
    }

  .repeater-card {
    background: rgba(255, 255, 255, 0.7); /* Slightly more opaque */
    border: 1px solid #cbd5e0;
    border-radius: 12px;
    padding: 25px; /* Increased padding */
    margin-bottom: 25px;
    transition: transform 0.2s ease;
  }

    .repeater-card:hover {
      border-color: #003366;
    }

  .repeater-actions {
    display: flex;
    justify-content: flex-start;
    margin-top: -5px;
    margin-bottom: 30px;
  }

  .card-title-row {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 15px;
    font-weight: bold;
    color: #003366;
  }

  .input-grid-3 {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 15px;
    margin-bottom: 10px;
  }

  .section-flex-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-end;
    margin-bottom: 10px;
  }

  .add-visitor-bottom-btn {
    background: #e6edf3; /* Light professional blue-grey */
    color: #003366;
    border: 1px solid #c9d6e4;
    padding: 10px 20px;
    border-radius: 8px;
    font-family: 'Montserrat', sans-serif;
    font-weight: 600;
    font-size: 0.9rem;
    cursor: pointer;
    display: flex;
    align-items: center;
    transition: all 0.2s ease;
  }

    .add-visitor-bottom-btn:hover {
      background: #003366;
      color: white;
      border-color: #003366;
      box-shadow: 0 4px 12px rgba(0, 51, 102, 0.15);
    }

    .add-visitor-bottom-btn:active {
      transform: scale(0.98);
    }

  .plus-icon {
    font-size: 1.2rem;
    font-weight: 800;
  }

  .add-btn-small {
    background: #003366;
    color: white;
    border: none;
    padding: 6px 12px;
    border-radius: 6px;
    font-size: 0.8rem;
    cursor: pointer;
    margin-bottom: 15px;
  }

  .remove-btn {
    color: #dc2626;
    background: none;
    border: 1px solid #dc2626;
    padding: 4px 10px;
    border-radius: 4px;
    cursor: pointer;
    font-size: 0.75rem;
  }

  .driver-selection-area {
    background: #edf2f7;
    padding: 10px;
    border-radius: 8px;
    margin-bottom: 15px;
    display: flex;
    align-items: center;
    gap: 10px;
  }

    .driver-selection-area label {
      color: #1a202c !important; /* Force the deep charcoal color */
      /*      font-weight: 600;*/
      cursor: pointer;
    }

    /* Optional: Improve the checkbox alignment while you're at it */
    .driver-selection-area input[type="checkbox"] {
      cursor: pointer;
      width: 16px;
      height: 16px;
      accent-color: #003366; /* Makes the checkbox match your theme blue */
    }

  .border-top {
    border-top: 1px dashed #cbd5e0;
    padding-top: 15px;
    margin-top: 15px;
  }

  .main-select {
    width: 100%;
    padding: 12px;
    border-radius: 8px;
    border: 1px solid #cbd5e0;
  }

    .main-select,
    .main-select option,
    input,
    textarea,
    button {
      font-family: 'Montserrat', sans-serif !important;
      font-size: 1rem;
      color: #1a202c; /* Ensure consistent text color */
    }

  .vehicle-card {
    margin-bottom: 20px;
  }

  @media (max-width: 1024px) {
    .registration-container {
      grid-template-columns: 1fr;
      gap: 40px;
      padding: 30px 20px;
    }

    .sticky-content {
      position: static;
    }

    .main-title {
      text-align: center;
      font-size: 2rem;
    }
  }

  @media (max-width: 600px) {
    .input-group-row {
      flex-direction: column;
      gap: 0;
    }

    .form-card {
      padding: 25px;
    }
  }

  .slide-enter-active, .slide-leave-active {
    transition: all 0.3s ease-out;
  }

  .slide-enter-from, .slide-leave-to {
    opacity: 0;
    transform: translateY(-10px);
  }

  /* Mobile Responsiveness: Stack columns on smaller screens */
  @media (max-width: 900px) {
    .logistics-grid {
      grid-template-columns: 1fr;
      gap: 0;
    }
  }

  @media (max-width: 850px) {
    .input-grid-3 {
      grid-template-columns: 1fr;
      gap: 0;
    }
  }

  @media (max-width: 768px) {
    /* Use 100% instead of 100vw to stay inside the scrollbar limits */
    .registration-wrapper,
    .registration-container,
    .form-card {
      width: 100% !important;
      max-width: 100% !important;
      padding-left: 12px !important;
      padding-right: 12px !important;
      margin: 0 !important;
      overflow-x: hidden;
    }

    .time-picker-grid {
      display: grid !important;
      /* 2fr for Hour, 2fr for Minute, 1.5fr for AM/PM creates a better visual balance */
      grid-template-columns: 2fr 2fr 1.5fr !important;
      gap: 8px !important;
      width: 100% !important;
      align-items: center;
    }

      .time-picker-grid select {
        /* REMOVE the margin-bottom entirely for the horizontal layout */
        margin-bottom: 0 !important;
        width: 100% !important;
        height: 45px; /* Force a consistent height for all three */
        padding: 0 20px 0 8px !important; /* Unified padding */
        font-size: 14px;
        background-position: right 4px center;
      }

      /* Optional: Center the text in the boxes for a cleaner look */
      .time-picker-grid .main-select {
        text-align: center;
      }

    /* This is the secret sauce: prevent any child from being wider than its parent */
    .form-card * {
      max-width: 100% !important;
    }

    /* Ensure no negative margins from grids are leaking */
    .input-grid-names,
    .logistics-grid,
    .input-grid-3 {
      display: block !important; /* Stack everything vertically */
      width: 100% !important;
      margin: 0 !important;
    }
  }
</style>
