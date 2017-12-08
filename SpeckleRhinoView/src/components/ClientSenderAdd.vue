<template>
  <v-dialog v-model='visible' style='width: 100%' >
    <v-card class=''>
      <v-card-title>
        Add Sender
      </v-card-title>        
      <v-card-text class="grey darken-4" text-center>
        <div class='step-1'>
          <v-form>
          <v-select label="Account" required v-bind:items='userAccounts' v-model='selectedAccountValue' style='z-index: 9000'autocomplete :search-input:sync='userAccounts'></v-select>
           <v-text-field label="Stream name" v-model="streamName" required></v-text-field>
          </v-form>
          <div v-if='objectSelection.length > 0' class='pa-1'>
            <p class='caption'>The following layers will be created:</p>
            <template v-for='sel in objectSelection'>
              <div class='caption'> 
                <v-chip small :style='{ color: sel.color }' class='eliptic'>{{sel.layer}}</v-chip> with {{sel.objectCount}} obj
              </div>
            </template>
          </div>
          <v-card v-else class='light-pink elevation-4 pa-2'>
            <v-icon>warning</v-icon>
            <span class='caption'>No selection found. Select some objects now to automatically populate your stream!</span>
          </v-card>
          <v-alert color='error' :value='fail' icon='error'>
            Failed to contact server.
            <br>
            {{ error }}
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn flat @click.native="visible=false">Cancel</v-btn>
        <v-btn color='light-blue' @click.native='addSender'>Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<script>
import API from '../store/apicaller.js'
import { EventBus } from '../event-bus'

export default {
  name: 'ClientSenderAdd',
  computed: {
    accounts() { 
      return this.$store.getters.accounts
    },
    userAccounts() {
      return this.$store.getters.accounts.map( a => a.serverName )
    },
    objectSelection() { return this.$store.getters.selection }
  },
  watch: {
    selectedAccountValue( value ) {
      if( !value ) return
      this.selectedAccount = this.accounts.find( ac => ac.serverName === value)
      API.getStreams( this.selectedAccount )
      .then( res => {
        this.fail = false
      })
      .catch( err => {
        this.fail = true
        this.error = err.toString()
      })
    },
    visible( value ) {
      if( value ) return
      this.selectedAccountValue = null
      this.selectedAccount = null
    }
  },
  data() {
    return {
      visible: false,
      selectedAccountValue: null,
      selectedAccount: null,
      fail: false,
      error: null,
      streamName: null,
    }
  },
  methods: {
    addSender() {
      
    }
  }, 
  mounted () {
   EventBus.$on('show-add-sender-dialog', () => {
      this.visible = true
    }) 
  }
}
</script>

<style lang="scss">
  .eliptic {
    width: 150px;
    text-overflow: ellipsis;
    white-space: nowrap;
    overflow: hidden;
  }
  .list__tile__title, .input-group__selections__comma {
    white-space: nowrap;
}
</style>