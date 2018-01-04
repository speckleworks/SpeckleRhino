<template>
  <v-dialog fullscreen transition='dialog-bottom-transition' v-model='visible' style='width: 100%'>
    <v-card class=''>
      <v-toolbar style="flex: 0 0 auto;" dark class='teal'>
        <v-btn icon @click.native="visible = false" dark>
          <v-icon>close</v-icon>
        </v-btn>
        <v-toolbar-title>Add Receiver</v-toolbar-title>
      </v-toolbar>
      <v-card-text text-center>
        <div class='step-1'>
          <v-select required label='Account' v-bind:items='selectItems' v-model='selectedAccountValue' style='z-index: 9000' autocomplete :search-input:sync='selectItems'></v-select>
          <br>
          <div class='headline grey--text' v-show='selectedAccountValue!=null'>
            <v-select label='Existing streams' v-bind:items='streamsMap' v-model='selectedStream' style='z-index: 9000' autocomplete :search-input.sync="streamsMap"></v-select>
            <v-text-field label='Stream Id Direct Input' v-model='directStreamId'></v-text-field>
          </div>
          <v-alert color='error' :value='fail' icon='error'>
            {{ error }}
          </v-alert>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer></v-spacer>
        <v-btn flat @click.native="visible=false">Cancel</v-btn>
        <v-btn color='light-blue' @click.native='addReceiver'>Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
<script>
import API from '../store/apicaller.js'
import { EventBus } from '../event-bus'

export default {
  name: 'ClientReceiverAdd',
  computed: {
    accounts( ) {
      return this.$store.getters.accounts
    },
    selectItems( ) {
      return this.$store.getters.accounts.map( a => a.serverName + ', ' + a.email )
    },
    streamsMap: {
      get( ) {
        return this.streams.map( s => s.name + this.separator + s.streamId )
      },
      set( value ) {

      }
    }
  },
  watch: {
    selectedAccountValue( value ) {
      if ( !value ) return
      this.selectedAccount = this.accounts.find( ac => { return ac.serverName === value.split(', ')[0] && ac.email === value.split(', ')[1] } )
      API.getStreams( this.selectedAccount )
        .then( res => {
          this.fail = false
          this.streams = res.streams
          this.selectedStream = null
        } )
        .catch( err => {
          this.streams = [ ]
          this.fail = true
          this.error = err.toString( )
        } )
    },
    visible( value ) {
      if ( value ) return
      this.selectedAccountValue = null
      this.selectedAccount = null
      this.selectedStream = null
      this.directStreamId = null
      this.streams = [ ]
    }
  },
  data( ) {
    return {
      visible: false,
      separator: ' | ',
      deleteDialog: false,
      selectedAccountValue: null,
      selectedAccount: null,
      selectedStream: null,
      directStreamId: null,
      streams: [ ],
      fail: false,
      error: null
    }
  },
  methods: {
    addReceiver( ) {
      let streamId = null
      if ( this.selectedStream ) streamId = this.selectedStream.split( this.separator )[ 1 ]
      else if ( this.directStreamId ) streamId = this.directStreamId
      if ( !streamId ) {
        this.fail = true
        this.error = 'No streamid provided.'
        return
      }
      console.log( this.$store.getters.clientByStreamId( streamId ) )
      if ( this.$store.getters.clientByStreamId( streamId ) ) {
        this.fail = true
        this.error = 'You already have a client sending/receiveing at that stream id. Duplicates not allowed!'
        return
      }

      let payload = {
        account: this.selectedAccount,
        streamId: streamId
      }
      Interop.addReceiverClient( JSON.stringify( payload ) )
      this.visible = false
    }
  },
  mounted( ) {
    EventBus.$on( 'show-add-receiver-dialog', ( ) => {
      this.visible = true
    } )
  }
}
</script>
<style lang="scss">
.list__tile__title {
  font-size: 12px !important;
}
</style>