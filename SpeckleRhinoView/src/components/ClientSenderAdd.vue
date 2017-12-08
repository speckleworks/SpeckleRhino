<template>
  <v-dialog v-model='visible' style='width: 100%' >
    <v-card class=''>
      <v-card-title>
        Add Sender
      </v-card-title>        
      <v-card-text class="grey darken-4" text-center>
        <div class='step-1'>
          <v-form>
          <v-select label="Account" required v-bind:items='selectItems' v-model='selectedAccountValue' style='z-index: 9000'autocomplete :search-input:sync='selectItems'></v-select>
           <v-text-field
              label="Stream name"
              v-model="streamName"
              required
            ></v-text-field>
          </v-form>
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
    selectItems() {
      return this.$store.getters.accounts.map( a => a.serverName )
    }
  },
  watch: {
    selectedAccountValue( value ) {
      if( !value ) return
      this.selectedAccount = this.accounts.find( ac => ac.serverName === value)
      API.getStreams( this.selectedAccount )
      .then( res => {
        this.fail = false
        this.streams = res.streams
        this.selectedStream = null
      })
      .catch( err => {
        this.streams = []
        this.fail = true
        this.error = err.toString()
      })
    },
    visible( value ) {
      if( value ) {
        Interop.getSelection()
         .then(res=>{
          console.log( res) 
         })
        return
      }
      this.selectedAccountValue = null
      this.selectedAccount = null
      this.selectedStream = null
      this.directStreamId = null
      this.streams = []
    }
  },
  data() {
    return {
      visible: false,
      separator: ' | ',
      deleteDialog: false,
      selectedAccountValue: null,
      selectedAccount: null,
      fail: false,
      error: null,
      streamName: null
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
  .list__tile__title, .input-group__selections__comma {
    white-space: nowrap;
}
</style>