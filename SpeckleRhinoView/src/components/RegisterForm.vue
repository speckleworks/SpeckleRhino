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
          <v-text-field label='server url'></v-text-field>
          <v-text-field label='your email address'></v-text-field>
          <v-text-field label='name'></v-text-field>
          <v-text-field label='surname'></v-text-field>
          <v-text-field label='password' type='password' min='8' hint='min 8 chars'></v-text-field>
          <v-text-field label='confirm' type='password' min='8' hint='min 8 chars'></v-text-field>
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
      visible: false
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