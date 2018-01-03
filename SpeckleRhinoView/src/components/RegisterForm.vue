<template>
  <v-dialog fullscreen transition='dialog-bottom-transition' v-model='visible' style='width: 100%'>
    <v-card>
      <v-toolbar style="flex: 0 0 auto;" dark class='light-blue'>
        <v-btn icon @click.native="visible = false" dark>
          <v-icon>close</v-icon>
        </v-btn>
        <v-toolbar-title>Register</v-toolbar-title>
      </v-toolbar>
      <v-card-text>
        Register a new account
        <v-form>
          <v-text-field v-model='serverUrl' label='server url'></v-text-field>
          <v-text-field v-model='userEmail' label='your email address' v-validate="'required|email'" :error-messages="errors.collect('email')" data-vv-name='email'></v-text-field>
          <v-text-field v-model='userName' label='name'></v-text-field>
          <v-text-field v-model='userSurname' label='surname'></v-text-field>
          <v-text-field v-model='password' label='password' type='password' min='8' hint='min 8 chars'></v-text-field>
          <v-text-field v-model='passwordConfirm' label='confirm' type='password' min='8' hint='min 8 chars'></v-text-field>
        </v-form>
      </v-card-text>
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
      serverUrl: null, userEmail: null, userName: null, userSurname: null, password: null, passwordConfirm: null
    }
  },
  methods: {},
  mounted( ) {
    console.log('asdf')
    EventBus.$on( 'show-register', ( ) => { this.visible = true } )
  }
}
</script>
<style lang="scss">
</style>