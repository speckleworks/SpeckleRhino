<template>
  <v-dialog fullscreen transition='dialog-bottom-transition' v-model='visible' style='width: 100%'>
    <v-card v-if='!showSuccessMessage'>
      <v-toolbar style="flex: 0 0 auto;" dark class='light-blue'>
        <v-btn icon @click.native='clear' dark>
          <v-icon>close</v-icon>
        </v-btn>
        <v-toolbar-title>Register</v-toolbar-title>
      </v-toolbar>
      <v-card-text class=''>
        <v-form>
          <v-text-field v-model='serverUrl' label='Speckle server api url' v-validate="'required'" :error-messages="errors.collect('url')" data-vv-name='url'></v-text-field>
          <v-text-field v-model='userEmail' label='Your email address' v-validate="'required|email'" :error-messages="errors.collect('email')" data-vv-name='email'></v-text-field>
          <v-text-field v-model='userName' label='Your name' v-validate="'required|max:20'" :error-messages="errors.collect('user_name')" data-vv-name='user_name'></v-text-field>
          <v-text-field v-model='userSurname' label='Your surname' v-validate="'max:20'" :error-messages="errors.collect('user_surname')" data-vv-name='user_surname'></v-text-field>
          <v-text-field v-model='password' label='Your account password.' type='password' hint='min 8 chars' v-validate="'required|min:8'" :error-messages="errors.collect('password')" data-vv-name="password"></v-text-field>
          <v-text-field v-model='passwordConfirm' label='confirm' type='password' hint='Passwords must match.' v-validate='{ is: password,  required: true }' :error-messages="errors.collect('password_confirm')" data-vv-name='password_confirm'></v-text-field>
          <v-alert color='error' :value='registrationError' icon='error'>
            {{ registrationError }}
          </v-alert>
        </v-form>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn @click='clear' color=''>Cancel</v-btn>
        <v-btn @click='submit' color='light-blue'>Register</v-btn>
      </v-card-actions>
    </v-card>
    <v-card v-else>
      <v-container fluid fill-height>
        <v-layout justify-center align-center>
          <div>
            <h4>Hello {{userName}}! You have successfuly registered an account.</h4>
            <p>When you will create a new sender or receiver, it will now be available.</p>
            <v-btn @click='clear' color='light-blue'>Great stuff!</v-btn>
          </div>
        </v-layout>
      </v-container>
    </v-card>
  </v-dialog>
</template>
<script>
import API from '../store/apicaller.js'
import { EventBus } from '../event-bus'

export default {
  name: 'RegisterForm',
  data( ) {
    return {
      visible: false,
      serverUrl: null,
      userEmail: null,
      userName: null,
      userSurname: null,
      password: null,
      passwordConfirm: null,
      registrationError: null,
      showSuccessMessage: false
    }
  },
  methods: {
    submit( ) {
      this.$validator.validateAll( ).then( result => {
        if ( result ) {
          let existing = this.$store.getters.accounts.find( ac => ac.restApi === this.serverUrl && ac.email === this.userEmail )
          if ( existing ) {
            this.registrationError = 'You already have an account on this server with this email.'
            return
          }
          this.registrationError = null
          let apiToken = null
          
          this.serverUrl = this.serverUrl.trim( )
          if ( !this.serverUrl.includes( 'api' ) )
            this.serverUrl += this.serverUrl.substr( -1 ) === '/' ? 'api' : '/api'

          API.registerAccount( { serverUrl: this.serverUrl, userEmail: this.userEmail, userName: this.userName, userSurname: this.userSurname, password: this.password } )
            .then( res => {
              apiToken = res.data.apitoken
              return API.getServerName( this.serverUrl )
            } )
            .then( res => {
              let payload = this.userEmail + ',' + apiToken + ',' + res + ',' + this.serverUrl + ',' + this.serverUrl
              Interop.addAccount( payload )
              this.$store.dispatch( 'getUserAccounts' )
              this.showSuccessMessage = true
            } )
            .catch( err => {
              this.registrationError = err.message
            } )
          return
        }
        this.registrationError = 'Please fix the errors in your form.'
      } )
    },
    clear( ) {
      this.serverUrl = null
      this.userEmail = null
      this.userName = null
      this.userSurname = null
      this.password = null
      this.passwordConfirm = null
      this.visible = false
      this.showSuccessMessage = false

      this.$validator.reset( )
    }
  },
  mounted( ) {
    EventBus.$on( 'show-register', ( ) => { this.visible = true } )
  }
}
</script>
<style lang="scss">
</style>